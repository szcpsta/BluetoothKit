// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Parser;

internal static class HciCommandPacketParser
{
    private enum Offset : byte
    {
        Opcode = 0,
        ParameterTotalLength = 2,
        Parameter = 3,
    }

    public static bool TryParse(ReadOnlyMemory<byte> p, out HciPacket parseResult)
    {
        var span = p.Span;

        if (span.Length < (int)Offset.Parameter)
        {
            parseResult = new HciUnknownPacket(new(HciPacketTypeValues.Command), p);
            return false;
        }

        if (span.Length != (byte)Offset.Parameter + span[(byte)Offset.ParameterTotalLength])
        {
            parseResult = new HciUnknownPacket(new(HciPacketTypeValues.Command), p);
            return false;
        }

        ushort opcode = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice((byte)Offset.Opcode, 2));
        ReadOnlyMemory<byte> parameters = p.Slice((int)Offset.Parameter);

        parseResult = new HciCommandPacket(new(opcode), parameters);
        return true;
    }
}
