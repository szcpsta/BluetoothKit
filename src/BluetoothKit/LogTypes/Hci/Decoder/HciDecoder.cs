// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Commands;
using BluetoothKit.LogTypes.Hci.Decoder.Events;
using BluetoothKit.LogTypes.Hci.Decoder.Vendor;

namespace BluetoothKit.LogTypes.Hci.Decoder;

public class HciDecoder
{
    private readonly HciCommandDecoder _commandDecoder;
    private readonly HciEventDecoder _eventDecoder;

    public HciDecoder() : this(new UnknownVendorDecoder())
    {
    }

    public HciDecoder(IVendorDecoder vendorDecoder)
    {
        _commandDecoder = new HciCommandDecoder(vendorDecoder);
        _eventDecoder = new HciEventDecoder(vendorDecoder);
    }

    public HciDecodedPacket Decode(HciPacket packet)
    {
        return packet switch
        {
            HciCommandPacket cmd => _commandDecoder.Decode(cmd),
            HciEventPacket evt => _eventDecoder.Decode(evt),
            _ => new HciUnknownDecodedPacket(packet)
        };
    }
}
