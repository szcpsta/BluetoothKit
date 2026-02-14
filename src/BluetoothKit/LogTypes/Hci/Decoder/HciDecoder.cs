// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Decoder;

public class HciDecoder
{
    private readonly HciCommandDecoder _commandDecoder = new HciCommandDecoder();
    private readonly HciEventDecoder _eventDecoder = new HciEventDecoder();

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
