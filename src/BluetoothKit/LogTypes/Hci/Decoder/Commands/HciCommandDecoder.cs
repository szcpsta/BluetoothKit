// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Vendor;

namespace BluetoothKit.LogTypes.Hci.Decoder.Commands;

public class HciCommandDecoder
{
    private readonly IVendorDecoder _vendorDecoder;

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

        if (packet.Opcode.Ogf == 0x04)
        {
            var decoded = InformationalParametersDecoder.DecodeCommand(packet);
            return new HciDecodedCommand(packet, decoded.Status, decoded.Name, decoded.Fields);
        }

        if (packet.Opcode.Ogf == 0x08)
        {
            var decoded = LeControllerCommandsDecoder.DecodeCommand(packet);
            return new HciDecodedCommand(packet, decoded.Status, decoded.Name, decoded.Fields);
        }

        return new HciDecodedCommand(packet, HciDecodeStatus.Unknown, "Unknown", Array.Empty<HciField>());
    }
}
