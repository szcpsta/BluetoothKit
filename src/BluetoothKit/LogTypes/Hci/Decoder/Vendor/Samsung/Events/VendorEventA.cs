// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;

namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung.Events;

internal static class VendorEventA
{
    internal const string EventAName = "Event A";

    private delegate DecodedResult DecodeEventAHandler(string name, ushort subeventCode, HciSpanReader span);

    private sealed record EventASpec(string Name, DecodeEventAHandler Decode);

    private static readonly Dictionary<ushort, EventASpec> Specs = new()
    {
        [0x0001] = new("Spec A", DecodeSpecA),
    };

    internal static DecodedResult Decode(HciSpanReader span)
    {
        if (!span.TryReadU16(out var subeventCode))
        {
            return new DecodedResult(EventAName, HciDecodeStatus.Invalid, Array.Empty<HciField>());
        }

        if (!Specs.TryGetValue(subeventCode, out var spec))
        {
            return new DecodedResult(
                $"{EventAName} (Subevent Code {HciValueFormatter.Hex16(subeventCode)})",
                HciDecodeStatus.Unknown,
                Array.Empty<HciField>());
        }

        return spec.Decode(spec.Name, subeventCode, span);
    }

    private static DecodedResult DecodeSpecA(string name, ushort subeventCode, HciSpanReader span)
    {
        return new(name, HciDecodeStatus.Invalid, Array.Empty<HciField>());
    }

}
