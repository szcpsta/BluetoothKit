// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BtSnoop.Parser;

internal class HciEventParser
{
    private enum Offset : byte
    {
        EventCode = 0,
        ParameterTotalLength = 1,
        Parameter = 2,
    }

    public static bool TryParse(ReadOnlySpan<byte> p, out HciPacket parseResult)
    {
        if (p.Length < (int)Offset.Parameter)
        {
            parseResult = new HciUnknownPacket(new(HciPacketTypeValues.Event), p);
            return false;
        }

        if (p.Length != (byte)Offset.Parameter + p[(byte)Offset.ParameterTotalLength])
        {
            parseResult = new HciUnknownPacket(new(HciPacketTypeValues.Event), p);
            return false;
        }

        byte eventCode = p[(byte)Offset.EventCode];
        ReadOnlySpan<byte> parameters = p.Slice((int)Offset.Parameter);

        parseResult = new HciEvent(new(eventCode), parameters);
        return true;
    }
}
