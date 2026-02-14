// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace BluetoothKit.LogTypes.Hci.Decoder.Formatters;

internal static class LeValueFormatter
{
    public static string Enable(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Disabled)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Enabled)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string AdvertisingType(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (ADV_IND, connectable undirected)",
            0x01 => $"{HciValueFormatter.Hex(value)} (ADV_DIRECT_IND, high duty cycle directed)",
            0x02 => $"{HciValueFormatter.Hex(value)} (ADV_SCAN_IND, scannable undirected)",
            0x03 => $"{HciValueFormatter.Hex(value)} (ADV_NONCONN_IND, non-connectable undirected)",
            0x04 => $"{HciValueFormatter.Hex(value)} (ADV_DIRECT_IND, low duty cycle directed)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string LegacyAdvertisingEventType(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (ADV_IND)",
            0x01 => $"{HciValueFormatter.Hex(value)} (ADV_DIRECT_IND)",
            0x02 => $"{HciValueFormatter.Hex(value)} (ADV_SCAN_IND)",
            0x03 => $"{HciValueFormatter.Hex(value)} (ADV_NONCONN_IND)",
            0x04 => $"{HciValueFormatter.Hex(value)} (SCAN_RSP)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string AdvertisingEventProperties(ushort value)
    {
        var flags = new List<string>();

        if ((value & 0x0001) != 0) flags.Add("Connectable");
        if ((value & 0x0002) != 0) flags.Add("Scannable");
        if ((value & 0x0004) != 0) flags.Add("Directed");
        if ((value & 0x0008) != 0) flags.Add("High Duty Cycle Directed");
        if ((value & 0x0010) != 0) flags.Add("Legacy PDUs");
        if ((value & 0x0020) != 0) flags.Add("Anonymous");
        if ((value & 0x0040) != 0) flags.Add("Include TxPower");
        if ((value & 0x0080) != 0) flags.Add("Use Decision PDUs");
        if ((value & 0x0100) != 0) flags.Add("Include AdvA in Decision PDUs");
        if ((value & 0x0200) != 0) flags.Add("Include ADI in Decision PDUs");

        var suffix = flags.Count == 0 ? "None" : string.Join(", ", flags);
        return $"{HciValueFormatter.Hex16(value)} ({suffix})";
    }

    public static string OwnAddressType(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Public Device Address)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Random Device Address)",
            0x02 => $"{HciValueFormatter.Hex(value)} (RPA from resolving list, fallback Public Address)",
            0x03 => $"{HciValueFormatter.Hex(value)} (RPA from resolving list, fallback Random Address)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string PeerAddressType(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Public Device or Public Identity Address)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Random Device or Random Identity Address)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string AddressType(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Public Device Address)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Random Device Address)",
            0x02 => $"{HciValueFormatter.Hex(value)} (Public Identity Address)",
            0x03 => $"{HciValueFormatter.Hex(value)} (Random Identity Address)",
            0xFF => $"{HciValueFormatter.Hex(value)} (Anonymous Address)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string DirectAddressType(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Public Device Address)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Non-resolvable Private or Static Random Address)",
            0x02 => $"{HciValueFormatter.Hex(value)} (Resolvable Private Address, resolved; Own_Address_Type 0x00/0x02)",
            0x03 => $"{HciValueFormatter.Hex(value)} (Resolvable Private Address, resolved; Own_Address_Type 0x01/0x03)",
            0xFE => $"{HciValueFormatter.Hex(value)} (Resolvable Private Address, unresolved)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string AdvertisingFilterPolicy(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Allow scan/connection from all devices)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Allow connection from all; scan from Filter Accept List)",
            0x02 => $"{HciValueFormatter.Hex(value)} (Allow scan from all; connection from Filter Accept List)",
            0x03 => $"{HciValueFormatter.Hex(value)} (Allow scan/connection from Filter Accept List only)",
            0x7F => $"{HciValueFormatter.Hex(value)} (Host has no preference)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string ScanningFilterPolicy(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Basic unfiltered)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Basic filtered)",
            0x02 => $"{HciValueFormatter.Hex(value)} (Extended unfiltered)",
            0x03 => $"{HciValueFormatter.Hex(value)} (Extended filtered)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string ScanType(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Passive)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Active)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string FilterDuplicates(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Disabled)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Enabled)",
            0x02 => $"{HciValueFormatter.Hex(value)} (Enabled, reset per scan period)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string AdvertisingChannelMap(byte value)
    {
        var channels = new List<string>();
        if ((value & 0x01) != 0) channels.Add("37");
        if ((value & 0x02) != 0) channels.Add("38");
        if ((value & 0x04) != 0) channels.Add("39");

        var suffix = channels.Count == 0 ? "None" : string.Join(",", channels);
        return $"{HciValueFormatter.Hex(value)} ({suffix})";
    }

    public static string PrimaryAdvertisingPhy(byte value)
        => value switch
        {
            0x01 => $"{HciValueFormatter.Hex(value)} (LE 1M)",
            0x03 => $"{HciValueFormatter.Hex(value)} (LE Coded)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string SecondaryAdvertisingPhy(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (No secondary advertising)",
            0x01 => $"{HciValueFormatter.Hex(value)} (LE 1M)",
            0x02 => $"{HciValueFormatter.Hex(value)} (LE 2M)",
            0x03 => $"{HciValueFormatter.Hex(value)} (LE Coded)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string ScanningPhys(byte value)
    {
        var phys = new List<string>();
        if ((value & 0x01) != 0) phys.Add("LE 1M");
        if ((value & 0x04) != 0) phys.Add("LE Coded");

        var suffix = phys.Count == 0 ? "None" : string.Join(", ", phys);
        return $"{HciValueFormatter.Hex(value)} ({suffix})";
    }

    public static string Operation(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Intermediate fragment)",
            0x01 => $"{HciValueFormatter.Hex(value)} (First fragment)",
            0x02 => $"{HciValueFormatter.Hex(value)} (Last fragment)",
            0x03 => $"{HciValueFormatter.Hex(value)} (Complete)",
            0x04 => $"{HciValueFormatter.Hex(value)} (Unchanged data)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string FragmentPreference(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Controller may fragment)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Controller should not fragment)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string ScanRequestNotificationEnable(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (Disabled)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Enabled)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string PhyOptions(byte value)
        => value switch
        {
            0x00 => $"{HciValueFormatter.Hex(value)} (No preference)",
            0x01 => $"{HciValueFormatter.Hex(value)} (Prefer S=2 coding)",
            0x02 => $"{HciValueFormatter.Hex(value)} (Prefer S=8 coding)",
            0x03 => $"{HciValueFormatter.Hex(value)} (Require S=2 coding)",
            0x04 => $"{HciValueFormatter.Hex(value)} (Require S=8 coding)",
            _ => HciValueFormatter.Hex(value),
        };

    public static string AdvertisingSid(byte value)
        => value == 0xFF ? $"{HciValueFormatter.Hex(value)} (No ADI field)" : HciValueFormatter.Hex(value);

    public static string PeriodicAdvertisingInterval(ushort value)
        => value == 0x0000 ? $"{HciValueFormatter.Hex16(value)} (No periodic advertising)" : HciValueFormatter.Hex16(value);

    public static string Interval625us(ushort value)
    {
        var ms = value * 0.625;
        return $"{HciValueFormatter.Hex16(value)} ({ms.ToString("0.###", CultureInfo.InvariantCulture)} ms)";
    }

    public static string Interval625us(uint value)
    {
        var ms = value * 0.625;
        return $"{HciValueFormatter.Hex24(value)} ({ms.ToString("0.###", CultureInfo.InvariantCulture)} ms)";
    }

    public static string AdvertisingTxPower(sbyte value)
    {
        var raw = unchecked((byte)value);
        if (raw == 0x7F)
            return $"{value} dBm (Host has no preference)";

        return HciValueFormatter.Dbm(value);
    }

    public static string Dbm(sbyte value)
    {
        var raw = unchecked((byte)value);
        if (raw == 0x7F)
            return $"{value} dBm (Not available)";

        return HciValueFormatter.Dbm(value);
    }

    public static string ExtendedAdvertisingEventType(ushort value)
    {
        var flags = new List<string>();

        if ((value & 0x0001) != 0) flags.Add("Connectable");
        if ((value & 0x0002) != 0) flags.Add("Scannable");
        if ((value & 0x0004) != 0) flags.Add("Directed");
        if ((value & 0x0008) != 0) flags.Add("Scan Response");
        if ((value & 0x0010) != 0) flags.Add("Legacy");

        var dataStatus = (value >> 5) & 0x03;
        var dataStatusText = dataStatus switch
        {
            0x00 => "Complete",
            0x01 => "Incomplete, more data",
            0x02 => "Incomplete, truncated",
            _ => "Reserved",
        };
        flags.Add($"Data Status: {dataStatusText}");

        var suffix = flags.Count == 0 ? "None" : string.Join(", ", flags);
        return $"{HciValueFormatter.Hex16(value)} ({suffix})";
    }
}
