// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BtSnoop.Parser;

public static class HciPacketParser
{
    private enum Offset : byte
    {
        PacketType = 0,
        Parameter = 1,
    }

    public static bool TryParse(ReadOnlySpan<byte> p, out HciPacket parseResult)
    {
        if (p.Length < (int)Offset.Parameter)
        {
            parseResult = new HciUnknownPacket(default, ReadOnlySpan<byte>.Empty);
            return false;
        }

        var packetTypeValue = p[(byte)Offset.PacketType];
        var parameters = p.Slice((byte)Offset.Parameter);

        switch (packetTypeValue)
        {
            case HciPacketTypeValues.Command: return HciCommandParser.TryParse(parameters, out parseResult);
            case HciPacketTypeValues.Acl:
                parseResult = new HciAclPacket(parameters);
                return true;
            case HciPacketTypeValues.Sco:
                parseResult = new HciScoPacket(parameters);
                return true;
            case HciPacketTypeValues.Event: return HciEventParser.TryParse(parameters, out parseResult);
            case HciPacketTypeValues.Iso:
                parseResult = new HciIsoPacket(parameters);
                return true;
            default:
                parseResult = new HciUnknownPacket(new(packetTypeValue), parameters);
                return false;
        }
    }
}
