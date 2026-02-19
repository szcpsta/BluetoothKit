// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder;

namespace BluetoothKit.Tests.LogTypes.Hci.Decoder;

public class HciDecoderRoutingTests
{
    [Fact]
    public void Decode_Command_Success()
    {
        HciCommandPacket packet = new(new HciOpcode(0x1001), new byte[] { });
        var decoded = new HciDecoder().Decode(packet);

        var command = Assert.IsType<HciDecodedCommand>(decoded);
        Assert.Equal(HciDecodeStatus.Success, command.Status);
        Assert.Equal("Read Local Version Information", command.Name);
    }

    [Fact]
    public void Decode_Command_Invalid()
    {
        HciCommandPacket packet = new(new HciOpcode(0x1001), new byte[] { 0x00 });
        var decoded = new HciDecoder().Decode(packet);

        var command = Assert.IsType<HciDecodedCommand>(decoded);
        Assert.Equal(HciDecodeStatus.Invalid, command.Status);
        Assert.Equal("Read Local Version Information", command.Name);
    }

    [Fact]
    public void Decode_Command_Unknown()
    {
        const ushort unknownOgf = 0x3D;

        HciCommandPacket packet = new(new HciOpcode((unknownOgf << 10) | 0x0001), new byte[] { 0x00 });
        var decoded = new HciDecoder().Decode(packet);

        var command = Assert.IsType<HciDecodedCommand>(decoded);
        Assert.Equal(HciDecodeStatus.Unknown, command.Status);
        Assert.Equal("Unknown", command.Name);
    }

    [Fact]
    public void Decode_Event_Success()
    {
        HciEventPacket packet = new(new HciEventCode(0x0F), new byte[] { 0x00, 0x01, 0x43, 0x20 });
        var decoded = new HciDecoder().Decode(packet);

        var evt = Assert.IsType<HciDecodedEvent>(decoded);
        Assert.Equal(HciDecodeStatus.Success, evt.Status);
        Assert.Equal("Command Status", evt.Name); // Command Status (LE Extended Create Connection [v1])
    }

    [Fact]
    public void Decode_EventWithSubevent_Success()
    {
        HciEventPacket packet = new(new HciEventCode(0x3E),
            new byte[]
            {
                0xd, 0x1, 0x10, 0x0, 0x1, 0x51, 0x13, 0x5e, 0xd8, 0x74, 0x7d, 0x1, 0x0, 0xff, 0x7f, 0xd9, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1b, 0x2, 0x1, 0x1a, 0x17, 0xff, 0x4c, 0x0, 0x9, 0x8, 0x13, 0x2,
                0xc0, 0xa8, 0x23, 0x79, 0x1b, 0x58, 0x16, 0x8, 0x0, 0x83, 0xc0, 0xb2, 0x3e, 0xb6, 0x4a, 0xb1
            });
        var decoded = new HciDecoder().Decode(packet);

        var evt = Assert.IsType<HciDecodedEvent>(decoded);
        Assert.Equal(HciDecodeStatus.Success, evt.Status);
        Assert.Equal("LE Extended Advertising Report", evt.Name); // LE Meta (LE Extended Advertising Report)
    }

    [Fact]
    public void Decode_Event_Invalid()
    {
        HciEventPacket packet = new(new HciEventCode(0x0F), new byte[] { 0x00, 0x01, 0x43 });
        var decoded = new HciDecoder().Decode(packet);

        var evt = Assert.IsType<HciDecodedEvent>(decoded);
        Assert.Equal(HciDecodeStatus.Invalid, evt.Status);
        Assert.Equal("Command Status", evt.Name);  // Command Status (LE Extended Create Connection [v1])
    }

    [Fact]
    public void Decode_EventWithInvalidSubevent_Invalid()
    {
        HciEventPacket packet = new(new HciEventCode(0x3E), new byte[] { });
        var decoded = new HciDecoder().Decode(packet);

        var evt = Assert.IsType<HciDecodedEvent>(decoded);
        Assert.Equal(HciDecodeStatus.Invalid, evt.Status);
        Assert.Equal("LE Meta", evt.Name); // LE Meta (LE Extended Advertising Report)
    }

    [Fact]
    public void Decode_EventWithTooLongSubevent_Invalid()
    {
        HciEventPacket packet = new(new HciEventCode(0x3E),
            new byte[]
            {
                0xd, 0x1, 0x10, 0x0, 0x1, 0x51, 0x13, 0x5e, 0xd8, 0x74, 0x7d, 0x1, 0x0, 0xff, 0x7f, 0xd9, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1b, 0x2, 0x1, 0x1a, 0x17, 0xff, 0x4c, 0x0, 0x9, 0x8, 0x13, 0x2,
                0xc0, 0xa8, 0x23, 0x79, 0x1b, 0x58, 0x16, 0x8, 0x0, 0x83, 0xc0, 0xb2, 0x3e, 0xb6, 0x4a, 0xb1, 0xff
            });
        var decoded = new HciDecoder().Decode(packet);

        var evt = Assert.IsType<HciDecodedEvent>(decoded);
        Assert.Equal(HciDecodeStatus.Invalid, evt.Status);
        Assert.Equal("LE Extended Advertising Report", evt.Name); // LE Meta (LE Extended Advertising Report)
    }

    [Fact]
    public void Decode_Event_Unknown()
    {
        const byte unknownEventCode = 0x3D;

        HciEventPacket packet = new(new HciEventCode(unknownEventCode), new byte[] { 0x00 });
        var decoded = new HciDecoder().Decode(packet);

        var evt = Assert.IsType<HciDecodedEvent>(decoded);
        Assert.Equal(HciDecodeStatus.Unknown, evt.Status);
        Assert.Equal("Unknown", evt.Name);
    }

    [Fact]
    public void Decode_EventWithUnknownSubevent_Unknown()
    {
        const byte unknownSubeventCode = 0xFF;

        HciEventPacket packet = new(new HciEventCode(0x3E), new byte[] { unknownSubeventCode });
        var decoded = new HciDecoder().Decode(packet);

        var evt = Assert.IsType<HciDecodedEvent>(decoded);
        Assert.Equal(HciDecodeStatus.Unknown, evt.Status);
        Assert.Equal("LE Meta (Subevent 0xFF)", evt.Name);
    }

    [Fact]
    public void Decode_UnknownPacketType_Unknown()
    {
        byte unknownPacketType = 0xFF;

        HciUnknownPacket packet = new(new HciPacketType(unknownPacketType), new byte[] { });
        var decoded = new HciDecoder().Decode(packet);

        var unknown = Assert.IsType<HciUnknownDecodedPacket>(decoded);
        Assert.Equal(HciDecodeStatus.Unknown, unknown.Status);
        Assert.Equal("Unknown packet type", unknown.Name);
    }

    [Fact]
    public void Decode_VendorCommand_Unknown()
    {
        HciCommandPacket packet = new(new HciOpcode(0xFD53), new byte[] { });
        var decoded = new HciDecoder().Decode(packet);

        var command = Assert.IsType<HciDecodedCommand>(decoded);
        Assert.Equal(HciDecodeStatus.Unknown, command.Status);
        Assert.Equal("Vendor Specific", command.Name); // Vendor Command 0x0153 (opcode 0xFD53)
    }

    [Fact]
    public void Decode_VendorEvent_Unknown()
    {
        HciEventPacket packet = new(new HciEventCode(0xFF),
            new byte[]
            {
                0x56, 0x4, 0x0, 0x0, 0x44, 0x50, 0x97, 0xc2, 0xe4, 0x10, 0x2, 0x80, 0xc0, 0x0, 0x0, 0x3, 0x2, 0x1,
                0x2, 0x0
            });
        var decoded = new HciDecoder().Decode(packet);

        var evt = Assert.IsType<HciDecodedEvent>(decoded);
        Assert.Equal(HciDecodeStatus.Unknown, evt.Status);
        Assert.Equal("Vendor Specific", evt.Name); // Vendor Command 0x0153 (opcode 0xFD53)
    }
}
