// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Decoder.Commands;

internal static class InformationalParametersDecoder
{
    internal sealed record DecodedCommand(string Name, HciDecodeStatus Status, IReadOnlyList<HciField> Fields);

    internal static bool TryDecodeCommand(HciCommandPacket packet, out DecodedCommand decoded)
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

    private static DecodedCommand DecodeReadLocalExtendedFeaturesCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var pageNumber) || !span.IsEmpty)
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Page Number", FormatHex(pageNumber))
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedCommand DecodeReadLocalSupportedCodecCapabilitiesCommand(string name, ReadOnlySpan<byte> parameters)
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
            new("Codec ID", FormatHexBytes(codecId)),
            new("Logical Transport Type", FormatLogicalTransportType(transportType)),
            new("Direction", FormatDirection(direction)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedCommand DecodeReadLocalSupportedControllerDelayCommand(string name, ReadOnlySpan<byte> parameters)
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
            new("Codec ID", FormatHexBytes(codecId)),
            new("Logical Transport Type", FormatLogicalTransportType(transportType)),
            new("Direction", FormatDirection(direction)),
            new("Codec Configuration Length", configLength.ToString()),
            new("Codec Configuration", FormatHexBytes(config)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedCommand DecodeNoParamsCommand(string name, ReadOnlySpan<byte> parameters)
    {
        if (parameters.IsEmpty)
            return new DecodedCommand(name, HciDecodeStatus.Success, Array.Empty<HciField>());

        return CreateInvalid(name);
    }

    private static DecodedCommand CreateInvalid(string name)
    {
        return new DecodedCommand(name, HciDecodeStatus.Invalid, Array.Empty<HciField>());
    }

    private static string FormatHex(byte value) => $"0x{value:X2}";

    private static string FormatHexBytes(ReadOnlySpan<byte> value)
        => value.IsEmpty ? "0x" : $"0x{Convert.ToHexString(value)}";

    private static string FormatLogicalTransportType(byte value)
    {
        return value switch
        {
            0x00 => "0x00 (BR/EDR ACL)",
            0x01 => "0x01 (BR/EDR SCO or eSCO)",
            0x02 => "0x02 (LE CIS)",
            0x03 => "0x03 (LE BIS)",
            _ => FormatHex(value),
        };
    }

    private static string FormatDirection(byte value)
    {
        return value switch
        {
            0x00 => "0x00 (Input)",
            0x01 => "0x01 (Output)",
            _ => FormatHex(value),
        };
    }
}
