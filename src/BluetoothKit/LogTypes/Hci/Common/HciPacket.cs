// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BluetoothKit.LogTypes.Hci.Common;

public abstract class HciPacket
{
    public HciPacketType PacketType { get; }

    internal HciPacket(HciPacketType packetType) => PacketType = packetType;
}

public sealed class HciCommandPacket : HciPacket
{
    public HciOpcode Opcode { get; }
    public ReadOnlyMemory<byte> Parameters { get; }

    internal HciCommandPacket(HciOpcode opcode, ReadOnlyMemory<byte> parameters) : base(new(HciPacketTypeValues.Command))
    {
        Opcode = opcode;
        Parameters = parameters;
    }
}

public sealed class HciAclPacket : HciPacket
{
    public ReadOnlyMemory<byte> Data { get; }

    internal HciAclPacket(ReadOnlyMemory<byte> data) : base(new(HciPacketTypeValues.Acl))
    {
        Data = data;
    }
}

public sealed class HciScoPacket : HciPacket
{
    public ReadOnlyMemory<byte> Data { get; }

    internal HciScoPacket(ReadOnlyMemory<byte> data) : base(new(HciPacketTypeValues.Sco))
    {
        Data = data;
    }
}

public sealed class HciEventPacket : HciPacket
{
    public HciEventCode EventCode { get; }
    public ReadOnlyMemory<byte> Parameters { get; }

    internal HciEventPacket(HciEventCode eventCode, ReadOnlyMemory<byte> parameters) : base(new(HciPacketTypeValues.Event))
    {
        EventCode = eventCode;
        Parameters = parameters;
    }
}

public sealed class HciIsoPacket : HciPacket
{
    public ReadOnlyMemory<byte> Data { get; }

    internal HciIsoPacket(ReadOnlyMemory<byte> data) : base(new(HciPacketTypeValues.Iso))
    {
        Data = data;
    }
}

public sealed class HciUnknownPacket : HciPacket
{
    public ReadOnlyMemory<byte> Data { get; }

    internal HciUnknownPacket(HciPacketType packetType, ReadOnlyMemory<byte> data) : base(packetType)
    {
        Data = data;
    }
}
