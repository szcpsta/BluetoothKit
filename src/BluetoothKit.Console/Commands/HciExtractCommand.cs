// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Parser;
using BluetoothKit.LogTypes.Hci.Reader;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BluetoothKit.Console.Commands;

internal sealed class HciExtractCommand : AsyncCommand<HciExtractCommand.Settings>
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
        [Description("Output file path (optional; defaults to <input>.<mode>)")]
        public string? OutputPath { get; set; }

        [CommandOption("-t|--types <TYPES>")]
        [Description("Packet types to include: cmd,acl,sco,evt,iso (comma-separated). Default: cmd,evt")]
        public string Types { get; set; } = "cmd,evt";

        internal IReadOnlyList<PacketKind> ParsedTypes { get; private set; } = Array.Empty<PacketKind>();

        public override ValidationResult Validate()
        {
            var modeLower = Mode?.ToLowerInvariant();

            if (modeLower != "console" && modeLower != "json")
                return ValidationResult.Error("--mode must be 'console' or 'json'.");

            if (modeLower == "console" && !string.IsNullOrWhiteSpace(OutputPath))
                return ValidationResult.Error("--out should not be specified when --mode=console.");

            if (!TryParseTypes(Types, out var parsedTypes, out var typesError))
                return ValidationResult.Error(typesError ?? "Invalid --types value.");

            ParsedTypes = parsedTypes;
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
        {
            WriteConsoleOutput(settings, result);
        }
        else
        {
            WriteJsonOutput(settings, result);
        }

        return 0;
    }

    private static async Task<ExtractResult> ExecuteExtractAsync(Settings settings, CancellationToken cancellationToken)
    {
        var selected = ResolveTypes(settings);

        List<long>? cmd = selected.Contains(PacketKind.Cmd) ? new List<long>() : null;
        List<long>? acl = selected.Contains(PacketKind.Acl) ? new List<long>() : null;
        List<long>? sco = selected.Contains(PacketKind.Sco) ? new List<long>() : null;
        List<long>? evt = selected.Contains(PacketKind.Evt) ? new List<long>() : null;
        List<long>? iso = selected.Contains(PacketKind.Iso) ? new List<long>() : null;

        long recordCount = 0;
        long frameNumber = 1;
        int parseFailures = 0;

        using var btsnoop = new BtSnoopReader(File.Open(settings.FilePath, FileMode.Open));
        await foreach (var record in btsnoop.ReadAsync(ct: cancellationToken))
        {
            recordCount++;
            if (!HciPacketParser.TryParse(record.PacketData, out var packet))
            {
                parseFailures++;
                frameNumber++;
                continue;
            }

            switch (packet)
            {
                case HciCommandPacket:
                    cmd?.Add(frameNumber);
                    break;
                case HciEventPacket:
                    evt?.Add(frameNumber);
                    break;
                case HciAclPacket:
                    acl?.Add(frameNumber);
                    break;
                case HciScoPacket:
                    sco?.Add(frameNumber);
                    break;
                case HciIsoPacket:
                    iso?.Add(frameNumber);
                    break;
            }

            frameNumber++;
        }

        return new ExtractResult
        {
            FilePath = Path.GetFileName(settings.FilePath),
            RecordCount = recordCount,
            ParseFailures = parseFailures,
            CmdFrameNumbers = cmd,
            AclFrameNumbers = acl,
            ScoFrameNumbers = sco,
            EvtFrameNumbers = evt,
            IsoFrameNumbers = iso,
        };
    }

    private static void WriteConsoleOutput(Settings settings, ExtractResult result)
    {
        if (settings.Verbose)
            AnsiConsole.MarkupLine($"[bold cyan]{result.FilePath}[/]");

        AnsiConsole.MarkupLine($"[bold green] Record Count   : {result.RecordCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] Parse Failures : {result.ParseFailures}[/]");

        WriteFrameList("cmd", result.CmdFrameNumbers);
        WriteFrameList("evt", result.EvtFrameNumbers);
        WriteFrameList("acl", result.AclFrameNumbers);
        WriteFrameList("sco", result.ScoFrameNumbers);
        WriteFrameList("iso", result.IsoFrameNumbers);
    }

    private static void WriteJsonOutput(Settings settings, ExtractResult result)
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
        JsonSerializer.Serialize(stream, result, options);

        AnsiConsole.MarkupLine($"[bold green] Output          : {outputPath}[/]");
    }

    private static void WriteFrameList(string label, List<long>? frameNumbers)
    {
        if (frameNumbers is null)
            return;

        string value = frameNumbers.Count == 0 ? "n/a" : string.Join(", ", frameNumbers);
        AnsiConsole.MarkupLine($"[bold green] {label,-3} : {value}[/]");
    }

    private sealed class ExtractResult
    {
        public string FilePath { get; init; } = string.Empty;
        public long RecordCount { get; init; }
        public int ParseFailures { get; init; }
        public List<long>? CmdFrameNumbers { get; init; }
        public List<long>? AclFrameNumbers { get; init; }
        public List<long>? ScoFrameNumbers { get; init; }
        public List<long>? EvtFrameNumbers { get; init; }
        public List<long>? IsoFrameNumbers { get; init; }
    }

    internal enum PacketKind
    {
        Cmd,
        Acl,
        Sco,
        Evt,
        Iso,
    }

    private static bool TryParseTypes(string? input, out List<PacketKind> types, out string? error)
    {
        types = new List<PacketKind>();
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "--types must include one or more of: cmd,acl,sco,evt,iso.";
            return false;
        }

        var values = input.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length == 0)
        {
            error = "--types must include one or more of: cmd,acl,sco,evt,iso.";
            return false;
        }

        var seen = new HashSet<PacketKind>();
        foreach (var value in values)
        {
            PacketKind kind;
            switch (value.ToLowerInvariant())
            {
                case "cmd":
                case "command":
                    kind = PacketKind.Cmd;
                    break;
                case "evt":
                case "event":
                    kind = PacketKind.Evt;
                    break;
                case "acl":
                    kind = PacketKind.Acl;
                    break;
                case "sco":
                    kind = PacketKind.Sco;
                    break;
                case "iso":
                    kind = PacketKind.Iso;
                    break;
                default:
                    error = $"Unknown type '{value}'. Use cmd,acl,sco,evt,iso.";
                    return false;
            }

            if (seen.Add(kind))
                types.Add(kind);
        }

        if (types.Count == 0)
        {
            error = "--types must include one or more of: cmd,acl,sco,evt,iso.";
            return false;
        }

        return true;
    }

    private static IReadOnlyList<PacketKind> ResolveTypes(Settings settings)
    {
        if (settings.ParsedTypes.Count != 0)
            return settings.ParsedTypes;

        if (TryParseTypes(settings.Types, out var parsed, out _))
            return parsed;

        return new[] { PacketKind.Cmd, PacketKind.Evt };
    }

    private static string ResolveOutputPath(Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OutputPath))
            return settings.OutputPath!;

        var directory = Path.GetDirectoryName(settings.FilePath);
        var fileName = Path.GetFileNameWithoutExtension(settings.FilePath);
        var extension = settings.Mode?.ToLowerInvariant() ?? "json";
        var outputFileName = $"{fileName}.{extension}";

        return string.IsNullOrEmpty(directory) ? outputFileName : Path.Combine(directory, outputFileName);
    }
}
