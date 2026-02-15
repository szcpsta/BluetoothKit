// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;

namespace BluetoothKit.LogTypes.Hci.Decoder.Commands;

internal static class InformationalParametersDecoder
{
    internal static bool TryDecodeCommand(HciCommandPacket packet, out DecodedResult decoded)
    {
        decoded = default!;

        var parameters = packet.Parameters.Span;
        switch (packet.Opcode.Ocf)
        {
            case 0x0001:
                decoded = DecodeNoParamsCommand("Read Local Version Information", parameters);
                return true;
            case 0x0002:
                decoded = DecodeNoParamsCommand("Read Local Supported Commands", parameters);
                return true;
            case 0x0003:
                decoded = DecodeNoParamsCommand("Read Local Supported Features", parameters);
                return true;
            case 0x0004:
                decoded = DecodeReadLocalExtendedFeaturesCommand("Read Local Extended Features", parameters);
                return true;
            case 0x0005:
                decoded = DecodeNoParamsCommand("Read Buffer Size", parameters);
                return true;
            case 0x0009:
                decoded = DecodeNoParamsCommand("Read BD_ADDR", parameters);
                return true;
            case 0x000A:
                decoded = DecodeNoParamsCommand("Read Data Block Size", parameters);
                return true;
            case 0x000B:
                decoded = DecodeNoParamsCommand("Read Local Supported Codecs [v1]", parameters);
                return true;
            case 0x000C:
                decoded = DecodeNoParamsCommand("Read Local Simple Pairing Options", parameters);
                return true;
            case 0x000D:
                decoded = DecodeNoParamsCommand("Read Local Supported Codecs [v2]", parameters);
                return true;
            case 0x000E:
                decoded = DecodeReadLocalSupportedCodecCapabilitiesCommand("Read Local Supported Codec Capabilities", parameters);
                return true;
            case 0x000F:
                decoded = DecodeReadLocalSupportedControllerDelayCommand("Read Local Supported Controller Delay", parameters);
                return true;
            default:
                return false;
        }
    }

    // OGF 0x04, OCF 0x0004
    private static DecodedResult DecodeReadLocalExtendedFeaturesCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var pageNumber) || !span.IsEmpty)
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Page Number", HciValueFormatter.Hex(pageNumber))
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x04, OCF 0x000E
    private static DecodedResult DecodeReadLocalSupportedCodecCapabilitiesCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);

        if (!span.TryReadBytes(5, out var codecId)
            || !span.TryReadU8(out var transportType)
            || !span.TryReadU8(out var direction)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Codec ID", HciValueFormatter.HexBytes(codecId)),
            new("Logical Transport Type", HciValueFormatter.LogicalTransportType(transportType)),
            new("Direction", HciValueFormatter.Direction(direction)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x04, OCF 0x000F
    private static DecodedResult DecodeReadLocalSupportedControllerDelayCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);

        if (!span.TryReadBytes(5, out var codecId)
            || !span.TryReadU8(out var transportType)
            || !span.TryReadU8(out var direction)
            || !span.TryReadU8(out var configLength)
            || !span.TryReadBytes(configLength, out var config)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Codec ID", HciValueFormatter.HexBytes(codecId)),
            new("Logical Transport Type", HciValueFormatter.LogicalTransportType(transportType)),
            new("Direction", HciValueFormatter.Direction(direction)),
            new("Codec Configuration Length", configLength.ToString()),
            new("Codec Configuration", HciValueFormatter.HexBytes(config)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x04, OCF 0x0001/0x0002/0x0003/0x0005/0x0009/0x000A/0x000B/0x000C/0x000D
    private static DecodedResult DecodeNoParamsCommand(string name, ReadOnlySpan<byte> parameters)
    {
        if (parameters.IsEmpty)
            return new DecodedResult(name, HciDecodeStatus.Success, Array.Empty<HciField>());

        return CreateInvalid(name);
    }

    private static DecodedResult CreateInvalid(string name)
    {
        return new DecodedResult(name, HciDecodeStatus.Invalid, Array.Empty<HciField>());
    }

}
