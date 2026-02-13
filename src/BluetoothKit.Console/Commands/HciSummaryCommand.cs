// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Parser;
using BluetoothKit.LogTypes.Hci.Reader;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BluetoothKit.Console.Commands;

internal sealed class HciSummaryCommand : AsyncCommand<HciSummaryCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("The file path to analyze")]
        public string FilePath { get; init; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] Input file not found: [yellow]{settings.FilePath}[/]");
            return -1;
        }

        await using var btsnoop = new BtSnoopReader(File.Open(settings.FilePath, FileMode.Open));
        AnsiConsole.MarkupLine("[bold yellow]HCI SUMMARY[/]");

        long recordCount = 0;
        long totalBytes = 0;
        long firstTimestamp = 0;
        long lastTimestamp = 0;
        DateTime? firstUtc = null;
        DateTime? lastUtc = null;

        int commandCount = 0;
        int eventCount = 0;
        int aclCount = 0;
        int scoCount = 0;
        int isoCount = 0;
        int unknownCount = 0;
        int parseFailures = 0;

        await foreach (var record in btsnoop.ReadAsync(ct: cancellationToken))
        {
            if (recordCount == 0)
            {
                firstTimestamp = record.TimestampMicros;
                firstUtc = record.GetDateTimeUtc();
            }

            lastTimestamp = record.TimestampMicros;
            lastUtc = record.GetDateTimeUtc();
            recordCount++;
            totalBytes += record.PacketData.Length;

            if (!HciPacketParser.TryParse(record.PacketData, out var packet))
            {
                parseFailures++;
                unknownCount++;
                continue;
            }

            switch (packet)
            {
                case HciCommandPacket:
                    commandCount++;
                    break;
                case HciEventPacket:
                    eventCount++;
                    break;
                case HciAclPacket:
                    aclCount++;
                    break;
                case HciScoPacket:
                    scoCount++;
                    break;
                case HciIsoPacket:
                    isoCount++;
                    break;
                default:
                    unknownCount++;
                    break;
            }
        }

        if (recordCount == 0)
        {
            AnsiConsole.MarkupLine("[bold yellow]No records found.[/]");
            return 0;
        }

        TimeSpan duration = TimeSpan.Zero;
        if (lastTimestamp >= firstTimestamp)
        {
            duration = TimeSpan.FromTicks((lastTimestamp - firstTimestamp) * 10);
        }

        AnsiConsole.MarkupLine($"[bold green] Record Count    : {recordCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] Total Bytes     : {totalBytes}[/]");
        AnsiConsole.MarkupLine($"[bold green] First (UTC)     : {firstUtc:O}[/]");
        AnsiConsole.MarkupLine($"[bold green] Last  (UTC)     : {lastUtc:O}[/]");
        AnsiConsole.MarkupLine($"[bold green] Duration        : {duration}[/]");
        AnsiConsole.MarkupLine($"[bold green] Commands        : {commandCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] Events          : {eventCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] ACL             : {aclCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] SCO             : {scoCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] ISO             : {isoCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] Unknown         : {unknownCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] Parse Failures  : {parseFailures}[/]");

        return 0;
    }
}
