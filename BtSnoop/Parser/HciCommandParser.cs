// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace BtSnoop.Parser;

internal class HciCommandParser
{
    private enum Offset : byte
    {
        Opcode = 0,
        ParameterTotalLength = 2,
        Parameter = 3,
    }

    public static bool TryParse(ReadOnlySpan<byte> p, out HciPacket parseResult)
    {
        if (p.Length < (int)Offset.Parameter)
        {
            parseResult = new HciUnknownPacket(new(HciPacketTypeValues.Command), p);
            return false;
        }

        if (p.Length != (byte)Offset.Parameter + p[(byte)Offset.ParameterTotalLength])
        {
            parseResult = new HciUnknownPacket(new(HciPacketTypeValues.Command), p);
            return false;
        }

        ushort opcode = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice((byte)Offset.Opcode, 2));
        ReadOnlySpan<byte> parameters = p.Slice((int)Offset.Parameter);

        parseResult = new HciCommand(new(opcode), parameters);
        return true;
    }
}
