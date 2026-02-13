// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BluetoothKit.LogTypes.Hci.Common;

public static class HciPacketTypeValues
{
    public const byte Command = 0x01;
    public const byte Acl = 0x02;
    public const byte Sco = 0x03;
    public const byte Event = 0x04;
    public const byte Iso = 0x05;
}

public readonly struct HciPacketType
{
    public byte Value { get; }
    public bool IsKnown => Value is HciPacketTypeValues.Command
        or HciPacketTypeValues.Acl
        or HciPacketTypeValues.Sco
        or HciPacketTypeValues.Event
        or HciPacketTypeValues.Iso;

    internal HciPacketType(byte value) => Value = value;

    public override string ToString() => $"0x{Value:X2}";
}

public readonly struct HciOpcode
{
    public ushort Value { get; }
    public byte Ogf => (byte)((Value >> 10) & 0x3F);
    public ushort Ocf => (ushort)(Value & 0x03FF);
    public bool IsVendorSpecific => Ogf == 0x3F;

    internal HciOpcode(ushort value) => Value = value;

    public override string ToString() => $"0x{Value:X4} (OGF={Ogf}, OCF={Ocf})";
}

public readonly struct HciEventCode
{
    public byte Value { get; }
    public bool IsVendorSpecific => Value == 0xFF;

    internal HciEventCode(byte value) => Value = value;

    public override string ToString() => $"0x{Value:X2}";
}
