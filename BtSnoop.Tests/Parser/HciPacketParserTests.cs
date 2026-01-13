// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BtSnoop.Parser;

namespace BtSnoop.Tests.Parser;

public class HciPacketParserTests
{
    [Fact]
    public void TryParse_TooShort_ReturnsFalse()
    {
        var parsed = HciPacketParser.TryParse([], out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_CommandPacket_ParsesOpcodeAndParameters()
    {
        byte[] packet = { 0x01, 0x34, 0x12, 0x02, 0xAA, 0xBB };

        var parsed = HciPacketParser.TryParse(packet, out var result);

        Assert.True(parsed);
        var command = Assert.IsType<HciCommand>(result);
        Assert.Equal(0x04, command.Opcode.Ogf);
        Assert.Equal(0x0234, command.Opcode.Ocf);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, command.Parameters.Span.ToArray());
    }

    [Fact]
    public void TryParse_CommandPacket_InvalidLength_ReturnsUnknown()
    {
        byte[] packet = { 0x01, 0x34, 0x12, 0x02, 0xAA };

        var parsed = HciPacketParser.TryParse(packet, out var result);

        Assert.False(parsed);
        var unknown = Assert.IsType<HciUnknownPacket>(result);
        Assert.Equal("0x01", unknown.PacketType.ToString());
        Assert.Equal(new byte[] { 0x34, 0x12, 0x02, 0xAA }, unknown.Data.Span.ToArray());
    }

    [Fact]
    public void TryParse_EventPacket_ParsesEventCodeAndParameters()
    {
        byte[] packet = { 0x04, 0x0E, 0x03, 0x01, 0x02, 0x03 };

        var parsed = HciPacketParser.TryParse(packet, out var result);

        Assert.True(parsed);
        var hciEvent = Assert.IsType<HciEvent>(result);
        Assert.Equal("0x0E", hciEvent.EventCode.ToString());
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, hciEvent.Parameters.Span.ToArray());
    }

    [Fact]
    public void TryParse_EventPacket_InvalidLength_ReturnsUnknown()
    {
        byte[] packet = { 0x04, 0x0E, 0x02, 0xFF };

        var parsed = HciPacketParser.TryParse(packet, out var result);

        Assert.False(parsed);
        var unknown = Assert.IsType<HciUnknownPacket>(result);
        Assert.Equal("0x04", unknown.PacketType.ToString());
        Assert.Equal(new byte[] { 0x0E, 0x02, 0xFF }, unknown.Data.Span.ToArray());
    }

    [Fact]
    public void TryParse_AclPacket_ReturnsAclPacket()
    {
        byte[] packet = { 0x02, 0x10, 0x20, 0x30 };

        var parsed = HciPacketParser.TryParse(packet, out var result);

        Assert.True(parsed);
        var acl = Assert.IsType<HciAclPacket>(result);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, acl.Data.Span.ToArray());
    }

    [Fact]
    public void TryParse_ScoPacket_ReturnsScoPacket()
    {
        byte[] packet = { 0x03, 0x10, 0x20, 0x30 };

        var parsed = HciPacketParser.TryParse(packet, out var result);

        Assert.True(parsed);
        var sco = Assert.IsType<HciScoPacket>(result);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, sco.Data.Span.ToArray());
    }

    [Fact]
    public void TryParse_IsoPacket_ReturnsIsoPacket()
    {
        byte[] packet = { 0x05, 0x10, 0x20, 0x30 };

        var parsed = HciPacketParser.TryParse(packet, out var result);

        Assert.True(parsed);
        var iso = Assert.IsType<HciIsoPacket>(result);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, iso.Data.Span.ToArray());
    }

    [Fact]
    public void TryParse_UnknownPacketType_ReturnsUnknownPacket()
    {
        byte[] packet = { 0x06, 0x10, 0x20 };

        var parsed = HciPacketParser.TryParse(packet, out var result);

        Assert.False(parsed);
        var unknown = Assert.IsType<HciUnknownPacket>(result);
        Assert.Equal(0x06, unknown.PacketType.Value);
        Assert.False(unknown.PacketType.IsKnown);
        Assert.Equal(new byte[] { 0x10, 0x20 }, unknown.Data.Span.ToArray());
    }

    [Fact]
    public void TryParse_UnknownPacketType_EmptyPayload_ReturnsUnknownPacket()
    {
        byte[] packet = { 0x06 };

        var parsed = HciPacketParser.TryParse(packet, out var result);

        Assert.False(parsed);
        var unknown = Assert.IsType<HciUnknownPacket>(result);
        Assert.Equal(0x06, unknown.PacketType.Value);
        Assert.Empty(unknown.Data.Span.ToArray());
    }
}
