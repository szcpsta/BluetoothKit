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

Filter HCI btsnoop/H4 logs (hex filters are required):

```bash
dotnet run --project src/BluetoothKit.Console -- hci filter <path> --eventcode 0x0E,0x0F
```

```bash
dotnet run --project src/BluetoothKit.Console -- hci filter <path> --ogf 0x04 --ocf 0x0001
```

```bash
dotnet run --project src/BluetoothKit.Console -- hci filter <path> --set 1
```

HCI filter options:

- `--set` predefined filter set id (see `--set 1` example below)
- `--ogf`, `--ocf`, `--opcode`, `--eventcode` (comma-separated, hex with `0x` prefix)
- `--le-subevent` LE Meta subevent filter (comma-separated, hex with `0x` prefix)
- `--mode` (`console` or `json`, default: `console`)
- `--out` output path (optional; required when `--mode json`, defaults to `<input>.json`; use `stdout` to write JSON to stdout)

If no filters are provided, the command prints a warning and produces no entries.

Filter set `--set 1` targets LE legacy + extended advertising/scan commands and related LE Meta subevents (0x3E with subevents 0x02, 0x0B, 0x0D, 0x11, 0x12, 0x13).

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

## HCI Decoder Design Notes

The HCI pipeline is split into parser and decoder responsibilities.

- Parser (`HciPacketParser`) validates structure (packet type and length). It returns `false` on invalid structure and produces an `HciUnknownPacket`.
- Decoder always returns a decoded result and uses `HciDecodeStatus` to describe semantic decode quality.

Status conventions:

- `Success`: fully decoded semantic fields
- `Unknown`: structure is valid but the command/event is not recognized
- `Invalid`: structure is not valid (e.g., parameter length mismatch)

Decode method pattern:

- Decoders use `Decode*` (not `TryDecode*`). The parser is the only place that returns a boolean.
- `Decode*` always returns a `DecodedResult` and uses `HciDecodeStatus.Unknown` when the structure is valid but the opcode/event/subevent is not recognized.
- `HciDecodeStatus.Invalid` is reserved for structurally invalid payloads after the packet has already been classified (e.g., length mismatches).

HCI packet formats (H4 payloads, notation: `[FieldName(size)]`):

- `HciCommandPacket`: `[Opcode(2)][ParamLength(1)][Parameters(N)]`
- `HciEventPacket`: `[EventCode(1)][ParamLength(1)][Parameters(N)]`

Raw H4 framing adds a 1-byte packet type prefix before the payload (e.g., command = `0x01`, event = `0x04`).

Unknown handling:

- Unknown packet types produce `HciUnknownPacket` at parse time and map to `HciUnknownDecodedPacket` with `Status=Unknown`.
- Unrecognized commands/events still produce `HciDecodedCommand`/`HciDecodedEvent` with `Status=Unknown`.

Filtering:

- Step 1 is "key filtering": fast extraction of stable identifiers (packet type, opcode, event code, subevent code) without full field decoding.
- Step 2 is "field filtering": uses decoded fields and is intentionally deferred until the key-filtering path is complete.

Field model:

- For now fields remain simple (`Name`, `Value`). A richer field schema (id/meta/value/display) will be introduced after key filtering is in place.

## Vendor-Specific HCI Decoders

Vendor-specific decoding is pluggable via `IVendorDecoder`.

- The default decoder is `UnknownVendorDecoder`, which labels vendor-specific traffic as `Vendor Specific`.
- Vendor implementations live under `src/BluetoothKit/LogTypes/Hci/Decoder/Vendor/<Vendor>` and can split command/event logic into `Commands/` and `Events/` subfolders.

Example:

```csharp
using BluetoothKit.LogTypes.Hci.Decoder;
using BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung;

var decoder = new HciDecoder(new SamsungVendorDecoder());
```

## Publish (Deployment)

The console app can be published as a self-contained binary. Choose the RID that matches your runtime.

Linux x64:

```bash
dotnet publish src/BluetoothKit.Console/BluetoothKit.Console.csproj -c Release -r linux-x64 --self-contained true -o ./publish
```

Linux arm64:

```bash
dotnet publish src/BluetoothKit.Console/BluetoothKit.Console.csproj -c Release -r linux-arm64 --self-contained true -o ./publish
```

Windows x64:

```bash
dotnet publish src/BluetoothKit.Console/BluetoothKit.Console.csproj -c Release -r win-x64 --self-contained true -o .\\publish
```

macOS Intel (x64):

```bash
dotnet publish src/BluetoothKit.Console/BluetoothKit.Console.csproj -c Release -r osx-x64 --self-contained true -o ./publish
```

macOS Apple Silicon (arm64):

```bash
dotnet publish src/BluetoothKit.Console/BluetoothKit.Console.csproj -c Release -r osx-arm64 --self-contained true -o ./publish
```

Run the published binary:

macOS / Linux:

```bash
./publish/BluetoothKit.Console hci filter --mode json --out stdout <path>
```

Windows (PowerShell):

```powershell
.\publish\BluetoothKit.Console.exe hci filter --mode json --out stdout <path>
```

Optional: ReadyToRun can slightly improve cold start at the cost of larger output size and longer publish time.

```bash
dotnet publish src/BluetoothKit.Console/BluetoothKit.Console.csproj -c Release -r linux-x64 --self-contained true -o ./publish -p:PublishReadyToRun=true
```
