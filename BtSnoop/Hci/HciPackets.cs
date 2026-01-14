// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BtSnoop.Hci;

public abstract class HciPacket
{
    public HciPacketType PacketType { get; }

    internal HciPacket(HciPacketType packetType) => PacketType = packetType;
}

public sealed class HciCommand : HciPacket
{
    public HciOpcode Opcode { get; }
    public ReadOnlyMemory<byte> Parameters { get; }

    internal HciCommand(HciOpcode opcode, ReadOnlySpan<byte> parameters) : base(new(HciPacketTypeValues.Command))
    {
        Opcode = opcode;
        Parameters = parameters.ToArray();
    }
}

public sealed class HciAclPacket : HciPacket
{
    public ReadOnlyMemory<byte> Data { get; }

    internal HciAclPacket(ReadOnlySpan<byte> data) : base(new(HciPacketTypeValues.Acl))
    {
        Data = data.ToArray();
    }
}

public sealed class HciScoPacket : HciPacket
{
    public ReadOnlyMemory<byte> Data { get; }

    internal HciScoPacket(ReadOnlySpan<byte> data) : base(new(HciPacketTypeValues.Sco))
    {
        Data = data.ToArray();
    }
}

public sealed class HciEvent : HciPacket
{
    public HciEventCode EventCode { get; }
    public ReadOnlyMemory<byte> Parameters { get; }

    internal HciEvent(HciEventCode eventCode, ReadOnlySpan<byte> parameters) : base(new(HciPacketTypeValues.Event))
    {
        EventCode = eventCode;
        Parameters = parameters.ToArray();
    }
}

public sealed class HciIsoPacket : HciPacket
{
    public ReadOnlyMemory<byte> Data { get; }

    internal HciIsoPacket(ReadOnlySpan<byte> data) : base(new(HciPacketTypeValues.Iso))
    {
        Data = data.ToArray();
    }
}

public sealed class HciUnknownPacket : HciPacket
{
    public ReadOnlyMemory<byte> Data { get; }

    internal HciUnknownPacket(HciPacketType packetType, ReadOnlySpan<byte> data) : base(packetType)
    {
        Data = data.ToArray();
    }
}
