// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using BluetoothKit.LogTypes.Power;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BluetoothKit.Console.Commands;

internal class ExtractCommand : Command<ExtractCommand.Settings>
{
    public class Settings : GlobalSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("The file path to analyze")]
        public string FilePath { get; init; } = string.Empty;

        [CommandOption("-m|--mode <MODE>")]
        [Description("Output mode: console or json")]
        public string Mode { get; set; } = "console";

        [CommandOption("--bucket")]
        [Description("Bucket Size. Default: 1")]
        public int Bucket { get; set; } = 1;

        [CommandOption("-o|--out <PATH>")]
        [Description("Output file path (required if mode=json)")]
        public string? OutputPath { get; set; }

        [CommandOption("-a|--agg|--aggregates <AGGREGATES>")]
        [Description("Aggregates to output: avg,min,max (comma-separated). Default: avg")]
        public string Aggregates { get; set; } = "avg";

        internal IReadOnlyList<AggregateKind> ParsedAggregates { get; private set; } = Array.Empty<AggregateKind>();

        public override ValidationResult Validate()
        {
            var modeLower = Mode?.ToLowerInvariant();

            if (modeLower != "console" && modeLower != "json")
                return ValidationResult.Error("--mode must be 'console' or 'json'.");

            if (modeLower == "json" && string.IsNullOrWhiteSpace(OutputPath))
                return ValidationResult.Error("--out is required when --mode=json.");

            if (modeLower == "console" && !string.IsNullOrWhiteSpace(OutputPath))
                return ValidationResult.Error("--out should not be specified when --mode=console.");

            if (Bucket <= 0)
                return ValidationResult.Error("--bucket must be a positive integer.");

            if (!TryParseAggregates(Aggregates, out var parsedAggregates, out var aggregatesError))
                return ValidationResult.Error(aggregatesError ?? "Invalid --aggregates value.");

            ParsedAggregates = parsedAggregates;

            return ValidationResult.Success();
        }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] Input file not found: [yellow]{settings.FilePath}[/]");
            return -1;
        }

        if (string.Equals(settings.Mode, "console", StringComparison.OrdinalIgnoreCase))
            ExcuteConsole(settings);
        else
            ExcuteJson(settings);

        return 0;
    }

    private void ExcuteConsole(Settings settings)
    {
        var aggregates = ResolveAggregates(settings);
        bool needAvg = aggregates.Contains(AggregateKind.Avg);
        bool needMin = aggregates.Contains(AggregateKind.Min);
        bool needMax = aggregates.Contains(AggregateKind.Max);

        using var pt5 = new Pt5Parser(File.OpenRead(settings.FilePath));
        for (long i = 0; i < pt5.SampleCount; i += settings.Bucket)
        {
            long last = Math.Min(i + settings.Bucket, pt5.SampleCount);

            long validCount = 0;
            double sum = 0;
            double min = double.MaxValue;
            double max = double.MinValue;
            for (long j = i; j < last; j++)
            {
                if (!pt5.TryGetCurrent(j, out var current))
                    continue;

                validCount++;
                if (needAvg)
                    sum += current;
                if (needMin && current < min)
                    min = current;
                if (needMax && current > max)
                    max = current;
            }

            double? avg = needAvg ? (validCount > 0 ? sum / validCount : null) : null;
            double? minValue = needMin ? (validCount > 0 ? min : null) : null;
            double? maxValue = needMax ? (validCount > 0 ? max : null) : null;

            var parts = new List<string>(aggregates.Count);
            foreach (var aggregate in aggregates)
            {
                switch (aggregate)
                {
                    case AggregateKind.Avg:
                        parts.Add($"avg={FormatValue(avg)}");
                        break;
                    case AggregateKind.Min:
                        parts.Add($"min={FormatValue(minValue)}");
                        break;
                    case AggregateKind.Max:
                        parts.Add($"max={FormatValue(maxValue)}");
                        break;
                }
            }

            AnsiConsole.MarkupLine($"{i,6} ~ {last - 1,6} : [bold green]{string.Join(" ", parts)}[/]");
        }
    }

    private void ExcuteJson(Settings settings)
    {
        var aggregates = ResolveAggregates(settings);
        bool needAvg = aggregates.Contains(AggregateKind.Avg);
        bool needMin = aggregates.Contains(AggregateKind.Min);
        bool needMax = aggregates.Contains(AggregateKind.Max);

        using var pt5 = new Pt5Parser(File.OpenRead(settings.FilePath));

        var output = new ExtractOutput
        {
            FilePath = Path.GetFileName(settings.FilePath),
            Bucket = settings.Bucket,
            SampleCount = pt5.SampleCount,
            Period = pt5.Period,
            AverageCurrent = pt5.AverageCurrent,
            CaptureDate = pt5.CaptureDate,
            AvgCurrents = needAvg ? new List<double?>() : null,
            MinCurrents = needMin ? new List<double?>() : null,
            MaxCurrents = needMax ? new List<double?>() : null,
        };

        for (long i = 0; i < pt5.SampleCount; i += settings.Bucket)
        {
            long last = Math.Min(i + settings.Bucket, pt5.SampleCount);

            long validCount = 0;
            double sum = 0;
            double min = double.MaxValue;
            double max = double.MinValue;
            for (long j = i; j < last; j++)
            {
                if (!pt5.TryGetCurrent(j, out var current))
                    continue;

                validCount++;
                if (needAvg)
                    sum += current;
                if (needMin && current < min)
                    min = current;
                if (needMax && current > max)
                    max = current;
            }

            double? avg = needAvg ? (validCount > 0 ? sum / validCount : null) : null;
            double? minValue = needMin ? (validCount > 0 ? min : null) : null;
            double? maxValue = needMax ? (validCount > 0 ? max : null) : null;
            if (needAvg)
                output.AvgCurrents!.Add(avg);
            if (needMin)
                output.MinCurrents!.Add(minValue);
            if (needMax)
                output.MaxCurrents!.Add(maxValue);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        var outputPath = settings.OutputPath ?? throw new InvalidOperationException("--out is required for json mode.");
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        using var stream = File.Create(outputPath);
        JsonSerializer.Serialize(stream, output, options);
    }

    private sealed class ExtractOutput
    {
        public string FilePath { get; init; } = string.Empty;
        public long SampleCount { get; init; }
        public double Period { get; init; }
        public double AverageCurrent { get; init; }
        public DateTime CaptureDate { get; init; }
        public int Bucket { get; init; }
        public List<double?>? AvgCurrents { get; init; }
        public List<double?>? MinCurrents { get; init; }
        public List<double?>? MaxCurrents { get; init; }
    }

    internal enum AggregateKind
    {
        Avg,
        Min,
        Max,
    }

    private static bool TryParseAggregates(string? input, out List<AggregateKind> aggregates, out string? error)
    {
        aggregates = new List<AggregateKind>();
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "--aggregates must include one or more of: avg,min,max.";
            return false;
        }

        var values = input.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length == 0)
        {
            error = "--aggregates must include one or more of: avg,min,max.";
            return false;
        }

        var seen = new HashSet<AggregateKind>();
        foreach (var value in values)
        {
            AggregateKind aggregate;
            switch (value.ToLowerInvariant())
            {
                case "avg":
                    aggregate = AggregateKind.Avg;
                    break;
                case "min":
                    aggregate = AggregateKind.Min;
                    break;
                case "max":
                    aggregate = AggregateKind.Max;
                    break;
                default:
                    error = $"Unknown aggregate '{value}'. Use avg,min,max.";
                    return false;
            }

            if (seen.Add(aggregate))
                aggregates.Add(aggregate);
        }

        if (aggregates.Count == 0)
        {
            error = "--aggregates must include one or more of: avg,min,max.";
            return false;
        }

        return true;
    }

    private static IReadOnlyList<AggregateKind> ResolveAggregates(Settings settings)
    {
        if (settings.ParsedAggregates.Count != 0)
            return settings.ParsedAggregates;

        if (TryParseAggregates(settings.Aggregates, out var parsed, out _))
            return parsed;

        return new[] { AggregateKind.Avg };
    }

    private static string FormatValue(double? value)
    {
        return value.HasValue ? value.Value.ToString("F3") : "n/a";
    }
}
