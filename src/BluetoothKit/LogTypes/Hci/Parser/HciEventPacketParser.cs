// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Parser;

internal static class HciEventPacketParser
{
    private enum Offset : byte
    {
        EventCode = 0,
        ParameterTotalLength = 1,
        Parameter = 2,
    }

    public static bool TryParse(ReadOnlyMemory<byte> p, out HciPacket parseResult)
    {
        var span = p.Span;

        if (span.Length < (int)Offset.Parameter)
        {
            parseResult = new HciUnknownPacket(new(HciPacketTypeValues.Event), p);
            return false;
        }

        if (span.Length != (byte)Offset.Parameter + span[(byte)Offset.ParameterTotalLength])
        {
            parseResult = new HciUnknownPacket(new(HciPacketTypeValues.Event), p);
            return false;
        }

        byte eventCode = span[(byte)Offset.EventCode];
        ReadOnlyMemory<byte> parameters = p.Slice((int)Offset.Parameter);

        parseResult = new HciEventPacket(new(eventCode), parameters);
        return true;
    }
}
