// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;

namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung.Commands;

internal static class OcfA
{
    internal const string OcfAName = "OCF A";

    private delegate DecodedResult DecodeOcfAHandler(string name, byte OcfACode, HciSpanReader span);

    private sealed record OcfASpec(string Name, DecodeOcfAHandler Decode);

    private static readonly Dictionary<byte, OcfASpec> Specs = new()
    {
        [0x01] = new("Spec A", DecodeSpecA),
    };

    internal static DecodedResult Decode(HciSpanReader span)
    {
        if (!span.TryReadU8(out var ocfACode))
        {
            return new DecodedResult(OcfAName, HciDecodeStatus.Invalid, Array.Empty<HciField>());
        }

        if (!Specs.TryGetValue(ocfACode, out var spec))
        {
            return new DecodedResult(
                $"{OcfAName} (OcfACode {HciValueFormatter.Hex(ocfACode)})",
                HciDecodeStatus.Unknown,
                Array.Empty<HciField>());
        }

        return spec.Decode(spec.Name, ocfACode, span);
    }

    private static DecodedResult DecodeSpecA(string name, byte ocfACode, HciSpanReader span)
    {
        return new(name, HciDecodeStatus.Invalid, Array.Empty<HciField>());
    }
}
