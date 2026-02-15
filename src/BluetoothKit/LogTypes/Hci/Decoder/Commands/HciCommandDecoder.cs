// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder;

namespace BluetoothKit.LogTypes.Hci.Decoder.Commands;

public class HciCommandDecoder
{
    public HciDecodedCommand Decode(HciCommandPacket packet)
    {
        if (packet.Opcode.Ogf == 0x04 && InformationalParametersDecoder.TryDecodeCommand(packet, out var decoded))
            return new HciDecodedCommand(packet, decoded.Status, decoded.Name, decoded.Fields);

        if (packet.Opcode.Ogf == 0x08 && LeControllerCommandsDecoder.TryDecodeCommand(packet, out var leDecoded))
            return new HciDecodedCommand(packet, leDecoded.Status, leDecoded.Name, leDecoded.Fields);

        return new HciDecodedCommand(packet, HciDecodeStatus.Unknown, "Unknown", Array.Empty<HciField>());
    }
}
