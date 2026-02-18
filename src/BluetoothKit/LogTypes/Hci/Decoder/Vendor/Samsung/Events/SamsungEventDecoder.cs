// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;
namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung.Events;

internal sealed class SamsungEventDecoder
{
    private static readonly Dictionary<ushort, Func<HciSpanReader, DecodedResult>> VendorEventDecoders = new()
    {
        [0x0001] = VendorEventA.Decode,
    };

    public HciDecodedEvent Decode(HciEventPacket packet)
    {
        var span = new HciSpanReader(packet.Parameters.Span);
        if (!span.TryReadU16(out var vendorEventCode))
        {
            return new HciDecodedEvent(packet, HciDecodeStatus.Invalid, VendorIds.Samsung, Array.Empty<HciField>());
        }

        if (VendorEventDecoders.TryGetValue(vendorEventCode, out var handler))
        {
            var decoded = handler(span);
            return new HciDecodedEvent(packet, decoded.Status, decoded.Name, decoded.Fields);
        }

        var name = $"{VendorIds.Samsung} (VendorEventCode {HciValueFormatter.Hex16(vendorEventCode)})";
        return new HciDecodedEvent(packet, HciDecodeStatus.Unknown, name, Array.Empty<HciField>());
    }
}
