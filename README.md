# BluetoothKit

BluetoothKit is a .NET library and CLI for parsing power log data (PT5) and producing summaries or aggregated outputs. The library also includes HCI (btsnoop/H4) readers and packet parsers.

## Prerequisites

- .NET SDK 9.0.x (the CI matrix uses 9.0.x)

## Project Structure

- `src/BluetoothKit` - core library (PT5 parser, power log types, HCI btsnoop reader + packet parsing)
- `src/BluetoothKit.Console` - CLI app built on Spectre.Console.Cli
- `tests/BluetoothKit.Tests` - xUnit test project
- `tests/TestData` - sample data used by tests

## Build and Test

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

## Formatting (Before Push)

CI verifies formatting with `dotnet format --verify-no-changes`. Run this locally before pushing:

```bash
dotnet format
```

If `dotnet format` is not available, install the tool:

```bash
dotnet tool install -g dotnet-format
```

## CLI Usage

Run the console app from the repo root:

```bash
dotnet run --project src/BluetoothKit.Console -- power summary <path>
```

Extract aggregated samples:

```bash
dotnet run --project src/BluetoothKit.Console -- power extract <path> \
  --agg avg,min,max \
  --bucket 100 \
  --mode json \
  --out ./output.json
```

Options:

- `--mode` (`console` or `json`, default: `console`)
- `--bucket` bucket size (default: `1`)
- `--agg` / `--aggregates` list of aggregates (`avg`, `min`, `max`, comma-separated)
- `--out` output path (required when `--mode json`)
