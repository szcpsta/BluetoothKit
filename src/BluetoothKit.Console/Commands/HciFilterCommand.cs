// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder;
using BluetoothKit.LogTypes.Hci.Parser;
using BluetoothKit.LogTypes.Hci.Reader;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BluetoothKit.Console.Commands;

internal sealed class HciFilterCommand : AsyncCommand<HciFilterCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("The file path to analyze")]
        public string FilePath { get; init; } = string.Empty;

        [CommandOption("-m|--mode <MODE>")]
        [Description("Output mode: console or json")]
        public string Mode { get; set; } = "console";

        [CommandOption("-o|--out <PATH>")]
        [Description("Output file path (optional; defaults to <input>.hci.json)")]
        public string? OutputPath { get; set; }

        [CommandOption("--set <ID>")]
        [Description("Filter set id (predefined filter presets)")]
        public int? SetId { get; set; }

        [CommandOption("--ogf <OGF>")]
        [Description("OGF filter (comma-separated, hex like 0x04)")]
        public string? Ogf { get; set; }

        [CommandOption("--ocf <OCF>")]
        [Description("OCF filter (comma-separated, hex like 0x0001)")]
        public string? Ocf { get; set; }

        [CommandOption("--opcode <OPCODE>")]
        [Description("Opcode filter (comma-separated, hex like 0x1001)")]
        public string? Opcode { get; set; }

        [CommandOption("--eventcode <EVENTCODE>")]
        [Description("Event code filter (comma-separated, hex like 0x0E)")]
        public string? EventCode { get; set; }

        [CommandOption("--le-subevent <LESUBEVENT>")]
        [Description("LE Meta subevent filter (comma-separated, hex like 0x02)")]
        public string? LeSubevent { get; set; }

        internal FilterSpec ParsedFilter { get; private set; } = FilterSpec.CreateDefault();

        public override ValidationResult Validate()
        {
            var modeLower = Mode?.ToLowerInvariant();
            if (modeLower != "console" && modeLower != "json")
                return ValidationResult.Error("--mode must be 'console' or 'json'.");

            if (modeLower == "console" && !string.IsNullOrWhiteSpace(OutputPath))
                return ValidationResult.Error("--out should not be specified when --mode=console.");

            if (!TryParseFilter(this, out var parsed, out var error))
                return ValidationResult.Error(error ?? "Invalid filter options.");

            ParsedFilter = parsed;
            return ValidationResult.Success();
        }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] Input file not found: [yellow]{settings.FilePath}[/]");
            return -1;
        }

        var result = await ExecuteExtractAsync(settings, cancellationToken);

        if (string.Equals(settings.Mode, "console", StringComparison.OrdinalIgnoreCase))
            WriteConsoleOutput(settings, result);
        else
            WriteJsonOutput(settings, result);

        return 0;
    }

    private static async Task<FilterOutput> ExecuteExtractAsync(Settings settings, CancellationToken cancellationToken)
    {
        var decoder = new HciDecoder();
        var entries = new List<FilterEntry>();

        long frameNumber = 1;
        int entryCount = 0;

        await using var btsnoop = new BtSnoopReader(File.Open(settings.FilePath, FileMode.Open));
        await foreach (var record in btsnoop.ReadAsync(ct: cancellationToken))
        {
            if (!HciPacketParser.TryParse(record.PacketData, out var packet))
            {
                frameNumber++;
                continue;
            }

            if (packet is HciCommandPacket commandPacket)
            {
                if (!settings.ParsedFilter.MatchesCommand(commandPacket.Opcode))
                {
                    frameNumber++;
                    continue;
                }

                var decoded = decoder.Decode(commandPacket) as HciDecodedCommand;
                if (decoded is null)
                {
                    frameNumber++;
                    continue;
                }

                var opcodeValue = decoded.RawCommand.Opcode.Value;
                var ogf = decoded.RawCommand.Opcode.Ogf;
                var ocf = decoded.RawCommand.Opcode.Ocf;

                entries.Add(new CommandEntry
                {
                    FrameNumber = frameNumber,
                    TimestampUtc = record.GetDateTimeUtc(),
                    Name = decoded.Name,
                    DecodeStatus = decoded.Status.ToString(),
                    Opcode = decoded.RawCommand.Opcode.ToString(),
                    OpcodeValue = FormatOpcode(opcodeValue),
                    OGF = FormatOgf(ogf),
                    OCF = FormatOcf(ocf),
                    Fields = decoded.Fields.ToList(),
                });
                entryCount++;

                frameNumber++;
                continue;
            }

            if (packet is HciEventPacket eventPacket)
            {
                var eventCode = eventPacket.EventCode.Value;
                if (!settings.ParsedFilter.MatchesEvent(eventPacket))
                {
                    frameNumber++;
                    continue;
                }

                var decoded = decoder.Decode(eventPacket) as HciDecodedEvent;
                if (decoded is null)
                {
                    frameNumber++;
                    continue;
                }

                entries.Add(new EventEntry
                {
                    FrameNumber = frameNumber,
                    TimestampUtc = record.GetDateTimeUtc(),
                    Name = decoded.Name,
                    DecodeStatus = decoded.Status.ToString(),
                    EventCode = FormatEventCode(eventCode),
                    Fields = decoded.Fields.ToList(),
                });
                entryCount++;

                frameNumber++;
                continue;
            }

            frameNumber++;
        }

        return new FilterOutput
        {
            FilePath = Path.GetFileName(settings.FilePath),
            EntryCount = entryCount,
            Filter = FilterOutputFilter.FromSpec(settings.ParsedFilter),
            Entries = entries,
        };
    }

    private static void WriteJsonOutput(Settings settings, FilterOutput output)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        var outputPath = ResolveOutputPath(settings);
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        using var stream = File.Create(outputPath);
        JsonSerializer.Serialize(stream, output, options);

        if (output.Filter?.IsEmpty == true)
            AnsiConsole.MarkupLine("[bold yellow]Warning:[/] No filter provided; no entries will match.");

        AnsiConsole.MarkupLine($"[bold green] Output          : {outputPath}[/]");
    }

    private static void WriteConsoleOutput(Settings settings, FilterOutput output)
    {
        if (settings.Verbose)
            AnsiConsole.MarkupLine($"[bold cyan]{output.FilePath}[/]");

        AnsiConsole.MarkupLine($"[bold green] Entry Count    : {output.EntryCount}[/]");

        if (output.Filter is not null)
        {
            var ogf = output.Filter.OGF?.Count > 0 ? string.Join(", ", output.Filter.OGF) : "n/a";
            var ocf = output.Filter.OCF?.Count > 0 ? string.Join(", ", output.Filter.OCF) : "n/a";
            var opcode = output.Filter.Opcode?.Count > 0 ? string.Join(", ", output.Filter.Opcode) : "n/a";
            var eventcode = output.Filter.EventCode?.Count > 0 ? string.Join(", ", output.Filter.EventCode) : "n/a";
            var leSubevent = output.Filter.LeSubevent?.Count > 0 ? string.Join(", ", output.Filter.LeSubevent) : "n/a";
            AnsiConsole.MarkupLine($"[bold green] Filter OGF     : {ogf}[/]");
            AnsiConsole.MarkupLine($"[bold green] Filter OCF     : {ocf}[/]");
            AnsiConsole.MarkupLine($"[bold green] Filter Opcode  : {opcode}[/]");
            AnsiConsole.MarkupLine($"[bold green] Filter Event   : {eventcode}[/]");
            AnsiConsole.MarkupLine($"[bold green] Filter LE Sub  : {leSubevent}[/]");
        }

        if (output.Filter?.IsEmpty == true)
            AnsiConsole.MarkupLine("[bold yellow]Warning:[/] No filter provided; no entries will match.");

        foreach (var entry in output.Entries)
        {
            switch (entry)
            {
                case CommandEntry cmd:
                    AnsiConsole.WriteLine(
                        $"{cmd.FrameNumber,6} {cmd.TimestampUtc:O} Command {cmd.OpcodeValue} {cmd.Name} [{cmd.DecodeStatus}]");
                    break;
                case EventEntry evt:
                    AnsiConsole.WriteLine(
                        $"{evt.FrameNumber,6} {evt.TimestampUtc:O} Event {evt.EventCode} {evt.Name} [{evt.DecodeStatus}]");
                    break;
                default:
                    AnsiConsole.WriteLine(
                        $"{entry.FrameNumber,6} {entry.TimestampUtc:O} ??? {entry.Name} [{entry.DecodeStatus}]");
                    break;
            }

            foreach (var field in entry.Fields)
                AnsiConsole.WriteLine($"        {field.Name} : {field.Value}");
        }
    }

    private static string ResolveOutputPath(Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OutputPath))
            return settings.OutputPath!;

        var directory = Path.GetDirectoryName(settings.FilePath);
        var fileName = Path.GetFileNameWithoutExtension(settings.FilePath);
        var outputFileName = $"{fileName}.json";

        return string.IsNullOrEmpty(directory) ? outputFileName : Path.Combine(directory, outputFileName);
    }

    private sealed class FilterOutput
    {
        public string FilePath { get; init; } = string.Empty;
        public int EntryCount { get; init; }
        public FilterOutputFilter? Filter { get; init; }
        public List<FilterEntry> Entries { get; init; } = new();
    }

    private sealed class FilterOutputFilter
    {
        public List<string>? OGF { get; init; }
        public List<string>? OCF { get; init; }
        public List<string>? Opcode { get; init; }
        public List<string>? EventCode { get; init; }
        public List<string>? LeSubevent { get; init; }
        public bool IsEmpty => (OGF is null || OGF.Count == 0)
                               && (OCF is null || OCF.Count == 0)
                               && (Opcode is null || Opcode.Count == 0)
                               && (EventCode is null || EventCode.Count == 0)
                               && (LeSubevent is null || LeSubevent.Count == 0);

        public static FilterOutputFilter FromSpec(FilterSpec spec) => new()
        {
            OGF = spec.Ogfs.Select(FormatOgf).OrderBy(x => x).ToList(),
            OCF = spec.Ocfs.Select(FormatOcf).OrderBy(x => x).ToList(),
            Opcode = spec.Opcodes.Select(FormatOpcode).OrderBy(x => x).ToList(),
            EventCode = spec.EventCodes.Select(FormatEventCode).OrderBy(x => x).ToList(),
            LeSubevent = spec.LeSubevents.Select(FormatEventCode).OrderBy(x => x).ToList(),
        };
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
    [JsonDerivedType(typeof(CommandEntry), "Command")]
    [JsonDerivedType(typeof(EventEntry), "Event")]
    private abstract class FilterEntry
    {
        public long FrameNumber { get; init; }
        public DateTime TimestampUtc { get; init; }
        public string Name { get; init; } = string.Empty;
        public string DecodeStatus { get; init; } = string.Empty;
        public List<HciField> Fields { get; init; } = new();
    }

    private sealed class CommandEntry : FilterEntry
    {
        public string Opcode { get; init; } = string.Empty;
        public string OpcodeValue { get; init; } = string.Empty;
        public string OGF { get; init; } = string.Empty;
        public string OCF { get; init; } = string.Empty;
    }

    private sealed class EventEntry : FilterEntry
    {
        public string EventCode { get; init; } = string.Empty;
    }

    private static bool TryParseFilter(Settings settings, out FilterSpec filter, out string? error)
    {
        error = null;

        FilterSpec baseFilter = FilterSpec.CreateDefault();
        if (settings.SetId is not null)
        {
            if (!HciFilterSets.TryGet(settings.SetId.Value, out var preset))
            {
                error = $"Unknown filter set id '{settings.SetId.Value}'. Available sets: {HciFilterSets.DescribeKnownSets()}";
                filter = FilterSpec.CreateDefault();
                return false;
            }

            baseFilter = preset.Spec;
        }

        if (!TryParseByteList(settings.Ogf, out var ogfs, out error, "ogf"))
        {
            filter = FilterSpec.CreateDefault();
            return false;
        }

        if (!TryParseUshortList(settings.Ocf, out var ocfs, out error, "ocf"))
        {
            filter = FilterSpec.CreateDefault();
            return false;
        }

        if (!TryParseUshortList(settings.Opcode, out var opcodes, out error, "opcode"))
        {
            filter = FilterSpec.CreateDefault();
            return false;
        }

        if (!TryParseByteList(settings.EventCode, out var eventCodes, out error, "eventcode"))
        {
            filter = FilterSpec.CreateDefault();
            return false;
        }

        foreach (var ocf in ocfs)
        {
            if (ocf > 0x03FF)
            {
                error = $"ocf '{FormatOcf(ocf)}' exceeds 10-bit range.";
                filter = FilterSpec.CreateDefault();
                return false;
            }
        }

        if (!TryParseByteList(settings.LeSubevent, out var leSubevents, out error, "le-subevent"))
        {
            filter = FilterSpec.CreateDefault();
            return false;
        }

        filter = baseFilter.Merge(new FilterSpec(ogfs, ocfs, opcodes, eventCodes, leSubevents));
        return true;
    }

    private static bool TryParseUshortList(string? input, out HashSet<ushort> values, out string? error, string label)
    {
        values = new HashSet<ushort>();
        error = null;

        if (string.IsNullOrWhiteSpace(input))
            return true;

        var parts = input.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (!TryParseUshort(part, out var value))
            {
                error = $"Invalid {label} '{part}'.";
                return false;
            }

            values.Add(value);
        }

        return true;
    }

    private static bool TryParseByteList(string? input, out HashSet<byte> values, out string? error, string label)
    {
        values = new HashSet<byte>();
        error = null;

        if (string.IsNullOrWhiteSpace(input))
            return true;

        var parts = input.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (!TryParseByte(part, out var value))
            {
                error = $"Invalid {label} '{part}'.";
                return false;
            }

            values.Add(value);
        }

        return true;
    }

    private static bool TryParseUshort(string value, out ushort result)
    {
        value = value.Trim();
        if (!value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            result = default;
            return false;
        }

        return ushort.TryParse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseByte(string value, out byte result)
    {
        value = value.Trim();
        if (!value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            result = default;
            return false;
        }

        return byte.TryParse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    private static string FormatOgf(byte ogf) => $"0x{ogf:X2}";
    private static string FormatOcf(ushort ocf) => $"0x{ocf:X4}";
    private static string FormatOpcode(ushort opcode) => $"0x{opcode:X4}";
    private static string FormatEventCode(byte value) => $"0x{value:X2}";
}
