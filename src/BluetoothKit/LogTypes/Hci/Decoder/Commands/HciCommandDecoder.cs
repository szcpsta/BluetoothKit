// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Vendor;

namespace BluetoothKit.LogTypes.Hci.Decoder.Commands;

public class HciCommandDecoder
{
    private readonly IVendorDecoder _vendorDecoder;

    private delegate DecodedResult OgfDecodeHandler(ushort ocf, HciSpanReader span);

    private static readonly Dictionary<byte, OgfDecodeHandler> OgfDecoders = new()
    {
        [0x04] = InformationalParametersDecoder.Decode,
        [0x08] = LeControllerCommandsDecoder.Decode,
    };

    public HciCommandDecoder() : this(new UnknownVendorDecoder())
    {
    }

    internal HciCommandDecoder(IVendorDecoder vendorDecoder)
    {
        _vendorDecoder = vendorDecoder;
    }

    public HciDecodedCommand Decode(HciCommandPacket packet)
    {
        if (packet.Opcode.IsVendorSpecific)
        {
            var vendorDecoded = _vendorDecoder.DecodeCommand(packet);
            return new HciDecodedCommand(packet, vendorDecoded.Status, vendorDecoded.Name, vendorDecoded.Fields);
        }

        if (OgfDecoders.TryGetValue(packet.Opcode.Ogf, out var handler))
        {
            var span = new HciSpanReader(packet.Parameters.Span);
            var decoded = handler(packet.Opcode.Ocf, span);
            return new HciDecodedCommand(packet, decoded.Status, decoded.Name, decoded.Fields);
        }

        return new HciDecodedCommand(packet, HciDecodeStatus.Unknown, "Unknown", Array.Empty<HciField>());
    }
}
