// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using BluetoothKit.LogTypes.Power;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BluetoothKit.Console.Commands;

internal class SummaryCommand : Command<SummaryCommand.Settings>
{
    public class Settings : GlobalSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("The file path to analyze")]
        public string FilePath { get; init; } = string.Empty;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] Input file not found: [yellow]{settings.FilePath}[/]");
            return -1;
        }

        using var pt5 = new Pt5Parser(File.Open(settings.FilePath, FileMode.Open));

        if (settings.Verbose == true)
        {
            AnsiConsole.MarkupLine($"[bold cyan]{Path.GetFileName(settings.FilePath)}[/]");
        }

        AnsiConsole.MarkupLine($"[bold green] SampleCount     : {pt5.SampleCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] Period          : {pt5.Period}[/]");
        AnsiConsole.MarkupLine($"[bold green] Average Current : {pt5.AverageCurrent:F3}[/]");
        AnsiConsole.MarkupLine($"[bold green] Capture Date    : {pt5.CaptureDate}[/]");

        return 0;
    }
}
