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
        [CommandArgument(0, "<name>")]
        [Description("The file name to analyze")]
        public string FileName { get; init; } = string.Empty;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.FileName))
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] Input file not found: [yellow]{settings.FileName}[/]");
            return -1;
        }

        using var pt5 = new Pt5Parser(File.Open(settings.FileName, FileMode.Open));

        if (settings.Verbose == true)
        {
            AnsiConsole.MarkupLine($"[bold cyan]{Path.GetFileName(settings.FileName)}[/]");
        }

        AnsiConsole.MarkupLine($"[bold green] SampleCount     : {pt5.SampleCount}[/]");
        AnsiConsole.MarkupLine($"[bold green] Period          : {pt5.Period}[/]");
        AnsiConsole.MarkupLine($"[bold green] Average Current : {pt5.AverageCurrent:F3}[/]");
        AnsiConsole.MarkupLine($"[bold green] Capture Date    : {pt5.CaptureDate}[/]");

        return 0;
    }
}
