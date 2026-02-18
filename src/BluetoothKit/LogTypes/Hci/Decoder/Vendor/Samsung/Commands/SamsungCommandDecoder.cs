// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung.Commands;

internal sealed class SamsungCommandDecoder
{
    private static readonly Dictionary<ushort, Func<HciSpanReader, DecodedResult>> OcfDecoders = new()
    {
        [0x0001] = OcfA.Decode,
    };

    public HciDecodedCommand Decode(HciCommandPacket packet)
    {
        var span = new HciSpanReader(packet.Parameters.Span);
        if (OcfDecoders.TryGetValue(packet.Opcode.Ocf, out var handler))
        {
            var decoded = handler(span);
            return new HciDecodedCommand(packet, decoded.Status, decoded.Name, decoded.Fields);
        }

        return new(packet, HciDecodeStatus.Unknown, VendorIds.Samsung, Array.Empty<HciField>());
    }
}
