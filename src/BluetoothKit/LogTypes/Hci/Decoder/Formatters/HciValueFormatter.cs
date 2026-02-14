// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BluetoothKit.LogTypes.Hci.Decoder.Formatters;

internal static class HciValueFormatter
{
    public static string Hex(byte value) => $"0x{value:X2}";
    public static string Hex16(ushort value) => $"0x{value:X4}";
    public static string Hex24(uint value) => $"0x{value:X6}";

    public static string HexBytes(ReadOnlySpan<byte> value)
        => value.IsEmpty ? "0x" : $"0x{Convert.ToHexString(value)}";

    public static string BdAddr(ReadOnlySpan<byte> value)
    {
        if (value.Length != 6)
            return HexBytes(value);

        var reversed = value.ToArray();
        Array.Reverse(reversed);
        return string.Join(":", reversed.Select(b => b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)));
    }

    public static string Dbm(sbyte value)
    {
        return $"{value} dBm";
    }

    public static string LogicalTransportType(byte value)
        => value switch
        {
            0x00 => "0x00 (BR/EDR ACL)",
            0x01 => "0x01 (BR/EDR SCO or eSCO)",
            0x02 => "0x02 (LE CIS)",
            0x03 => "0x03 (LE BIS)",
            _ => Hex(value),
        };

    public static string Direction(byte value)
        => value switch
        {
            0x00 => "0x00 (Input)",
            0x01 => "0x01 (Output)",
            _ => Hex(value),
        };
}
