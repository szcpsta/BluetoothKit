// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;

namespace BluetoothKit.LogTypes.Hci.Decoder.Commands;

internal static class InformationalParametersDecoder
{
    private delegate DecodedResult DecodeCommandHandler(string name, HciSpanReader span);

    private sealed record CommandSpec(string Name, DecodeCommandHandler Decode);

    private static readonly Dictionary<ushort, CommandSpec> Specs = new()
    {
        [0x0001] = new("Read Local Version Information", DecodeNoParamsCommand),
        [0x0002] = new("Read Local Supported Commands", DecodeNoParamsCommand),
        [0x0003] = new("Read Local Supported Features", DecodeNoParamsCommand),
        [0x0004] = new("Read Local Extended Features", DecodeReadLocalExtendedFeaturesCommand),
        [0x0005] = new("Read Buffer Size", DecodeNoParamsCommand),
        [0x0009] = new("Read BD_ADDR", DecodeNoParamsCommand),
        [0x000A] = new("Read Data Block Size", DecodeNoParamsCommand),
        [0x000B] = new("Read Local Supported Codecs [v1]", DecodeNoParamsCommand),
        [0x000C] = new("Read Local Simple Pairing Options", DecodeNoParamsCommand),
        [0x000D] = new("Read Local Supported Codecs [v2]", DecodeNoParamsCommand),
        [0x000E] = new("Read Local Supported Codec Capabilities", DecodeReadLocalSupportedCodecCapabilitiesCommand),
        [0x000F] = new("Read Local Supported Controller Delay", DecodeReadLocalSupportedControllerDelayCommand),
    };

    internal static DecodedResult DecodeCommand(HciCommandPacket packet)
    {
        if (!Specs.TryGetValue(packet.Opcode.Ocf, out var spec))
            return new DecodedResult("Unknown", HciDecodeStatus.Unknown, Array.Empty<HciField>());

        return spec.Decode(spec.Name, new HciSpanReader(packet.Parameters.Span));
    }

    private static DecodedResult DecodeReadLocalExtendedFeaturesCommand(string name, HciSpanReader span)
    {
        if (!span.TryReadU8(out var pageNumber) || !span.IsEmpty)
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Page Number", HciValueFormatter.Hex(pageNumber))
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeReadLocalSupportedCodecCapabilitiesCommand(string name, HciSpanReader span)
    {
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

    private static DecodedResult DecodeReadLocalSupportedControllerDelayCommand(string name, HciSpanReader span)
    {
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

    private static DecodedResult DecodeNoParamsCommand(string name, HciSpanReader span)
    {
        if (span.IsEmpty)
            return new DecodedResult(name, HciDecodeStatus.Success, Array.Empty<HciField>());

        return CreateInvalid(name);
    }

    private static DecodedResult CreateInvalid(string name)
        => new(name, HciDecodeStatus.Invalid, Array.Empty<HciField>());

}
