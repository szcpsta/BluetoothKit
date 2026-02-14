// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Decoder;

public abstract class HciDecodedPacket
{
    public HciPacket Raw { get; }
    public HciPacketType PacketType => Raw.PacketType;
    public HciDecodeStatus Status { get; }

    protected HciDecodedPacket(HciPacket raw, HciDecodeStatus status)
    {
        Raw = raw;
        Status = status;
    }
}

public sealed class HciDecodedCommand : HciDecodedPacket
{
    public HciCommandPacket RawCommand { get; }
    public string Name { get; }
    public IReadOnlyList<HciField> Fields { get; }

    internal HciDecodedCommand(HciCommandPacket raw, HciDecodeStatus status, string name, IReadOnlyList<HciField> fields)
        : base(raw, status)
    {
        RawCommand = raw;
        Name = name;
        Fields = fields;
    }
}

public class HciDecodedEvent : HciDecodedPacket
{
    public HciEventPacket RawEvent { get; }
    public string Name { get; }
    public IReadOnlyList<HciField> Fields { get; }

    internal HciDecodedEvent(HciEventPacket raw, HciDecodeStatus status, string name, IReadOnlyList<HciField> fields)
        : base(raw, status)
    {
        RawEvent = raw;
        Name = name;
        Fields = fields;
    }
}

public sealed class HciUnknownDecodedPacket : HciDecodedPacket
{
    public string Name => "Unknown packet type";

    internal HciUnknownDecodedPacket(HciPacket packet) : base(packet, HciDecodeStatus.Invalid)
    {
    }
}
