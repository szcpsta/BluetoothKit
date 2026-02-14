// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;

namespace BluetoothKit.LogTypes.Hci.Decoder.Commands;

internal static class LeControllerCommandsDecoder
{
    internal sealed record DecodedCommand(string Name, HciDecodeStatus Status, IReadOnlyList<HciField> Fields);

    internal static bool TryDecodeCommand(HciCommandPacket packet, out DecodedCommand decoded)
    {
        decoded = default!;

        var parameters = packet.Parameters.Span;
        switch (packet.Opcode.Ocf)
        {
            case 0x0006:
                decoded = DecodeSetAdvertisingParametersCommand("LE Set Advertising Parameters", parameters);
                return true;
            case 0x0007:
                decoded = DecodeNoParamsCommand("LE Read Advertising Physical Channel Tx Power", parameters);
                return true;
            case 0x0008:
                decoded = DecodeSetAdvertisingDataCommand("LE Set Advertising Data", parameters);
                return true;
            case 0x0009:
                decoded = DecodeSetScanResponseDataCommand("LE Set Scan Response Data", parameters);
                return true;
            case 0x000A:
                decoded = DecodeSetAdvertisingEnableCommand("LE Set Advertising Enable", parameters);
                return true;
            case 0x000B:
                decoded = DecodeSetScanParametersCommand("LE Set Scan Parameters", parameters);
                return true;
            case 0x000C:
                decoded = DecodeSetScanEnableCommand("LE Set Scan Enable", parameters);
                return true;
            case 0x0035:
                decoded = DecodeSetAdvertisingSetRandomAddressCommand("LE Set Advertising Set Random Address", parameters);
                return true;
            case 0x0036:
                decoded = DecodeSetExtendedAdvertisingParametersV1Command("LE Set Extended Advertising Parameters [v1]", parameters);
                return true;
            case 0x0037:
                decoded = DecodeSetExtendedAdvertisingDataCommand("LE Set Extended Advertising Data", parameters);
                return true;
            case 0x0038:
                decoded = DecodeSetExtendedScanResponseDataCommand("LE Set Extended Scan Response Data", parameters);
                return true;
            case 0x0039:
                decoded = DecodeSetExtendedAdvertisingEnableCommand("LE Set Extended Advertising Enable", parameters);
                return true;
            case 0x003A:
                decoded = DecodeNoParamsCommand("LE Read Maximum Advertising Data Length", parameters);
                return true;
            case 0x003B:
                decoded = DecodeNoParamsCommand("LE Read Number of Supported Advertising Sets", parameters);
                return true;
            case 0x003C:
                decoded = DecodeRemoveAdvertisingSetCommand("LE Remove Advertising Set", parameters);
                return true;
            case 0x003D:
                decoded = DecodeNoParamsCommand("LE Clear Advertising Sets", parameters);
                return true;
            case 0x0041:
                decoded = DecodeSetExtendedScanParametersCommand("LE Set Extended Scan Parameters", parameters);
                return true;
            case 0x0042:
                decoded = DecodeSetExtendedScanEnableCommand("LE Set Extended Scan Enable", parameters);
                return true;
            case 0x007F:
                decoded = DecodeSetExtendedAdvertisingParametersV2Command("LE Set Extended Advertising Parameters [v2]", parameters);
                return true;
            default:
                return false;
        }
    }

    // OGF 0x08, OCF 0x0006
    private static DecodedCommand DecodeSetAdvertisingParametersCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU16(out var advertisingIntervalMin)
            || !span.TryReadU16(out var advertisingIntervalMax)
            || !span.TryReadU8(out var advertisingType)
            || !span.TryReadU8(out var ownAddressType)
            || !span.TryReadU8(out var peerAddressType)
            || !span.TryReadBytes(6, out var peerAddress)
            || !span.TryReadU8(out var advertisingChannelMap)
            || !span.TryReadU8(out var advertisingFilterPolicy)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Advertising Interval Min", LeValueFormatter.Interval625us(advertisingIntervalMin)),
            new("Advertising Interval Max", LeValueFormatter.Interval625us(advertisingIntervalMax)),
            new("Advertising Type", LeValueFormatter.AdvertisingType(advertisingType)),
            new("Own Address Type", LeValueFormatter.OwnAddressType(ownAddressType)),
            new("Peer Address Type", LeValueFormatter.PeerAddressType(peerAddressType)),
            new("Peer Address", HciValueFormatter.BdAddr(peerAddress)),
            new("Advertising Channel Map", LeValueFormatter.AdvertisingChannelMap(advertisingChannelMap)),
            new("Advertising Filter Policy", LeValueFormatter.AdvertisingFilterPolicy(advertisingFilterPolicy)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x0008
    private static DecodedCommand DecodeSetAdvertisingDataCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var dataLength)
            || !span.TryReadBytes(31, out var data)
            || !span.IsEmpty
            || dataLength > 31)
        {
            return CreateInvalid(name);
        }

        var payload = dataLength == 0 ? ReadOnlySpan<byte>.Empty : data[..dataLength];
        var fields = new List<HciField>
        {
            new("Advertising Data Length", dataLength.ToString()),
            new("Advertising Data", FormatHexBytes(payload)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x0009
    private static DecodedCommand DecodeSetScanResponseDataCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var dataLength)
            || !span.TryReadBytes(31, out var data)
            || !span.IsEmpty
            || dataLength > 31)
        {
            return CreateInvalid(name);
        }

        var payload = dataLength == 0 ? ReadOnlySpan<byte>.Empty : data[..dataLength];
        var fields = new List<HciField>
        {
            new("Scan Response Data Length", dataLength.ToString()),
            new("Scan Response Data", FormatHexBytes(payload)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x000A
    private static DecodedCommand DecodeSetAdvertisingEnableCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var advertisingEnable) || !span.IsEmpty)
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Advertising Enable", LeValueFormatter.Enable(advertisingEnable)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x000B
    private static DecodedCommand DecodeSetScanParametersCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var scanType)
            || !span.TryReadU16(out var scanInterval)
            || !span.TryReadU16(out var scanWindow)
            || !span.TryReadU8(out var ownAddressType)
            || !span.TryReadU8(out var scanningFilterPolicy)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("LE Scan Type", LeValueFormatter.ScanType(scanType)),
            new("LE Scan Interval", LeValueFormatter.Interval625us(scanInterval)),
            new("LE Scan Window", LeValueFormatter.Interval625us(scanWindow)),
            new("Own Address Type", LeValueFormatter.OwnAddressType(ownAddressType)),
            new("Scanning Filter Policy", LeValueFormatter.ScanningFilterPolicy(scanningFilterPolicy)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x000C
    private static DecodedCommand DecodeSetScanEnableCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var scanEnable)
            || !span.TryReadU8(out var filterDuplicates)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("LE Scan Enable", LeValueFormatter.Enable(scanEnable)),
            new("Filter Duplicates", LeValueFormatter.FilterDuplicates(filterDuplicates)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x0035
    private static DecodedCommand DecodeSetAdvertisingSetRandomAddressCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var advertisingHandle)
            || !span.TryReadBytes(6, out var randomAddress)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Advertising Handle", FormatHex(advertisingHandle)),
            new("Random Address", HciValueFormatter.BdAddr(randomAddress)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x0036 (v1)
    private static DecodedCommand DecodeSetExtendedAdvertisingParametersV1Command(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var advertisingHandle)
            || !span.TryReadU16(out var advertisingEventProperties)
            || !span.TryReadU24(out var primaryAdvertisingIntervalMin)
            || !span.TryReadU24(out var primaryAdvertisingIntervalMax)
            || !span.TryReadU8(out var primaryAdvertisingChannelMap)
            || !span.TryReadU8(out var ownAddressType)
            || !span.TryReadU8(out var peerAddressType)
            || !span.TryReadBytes(6, out var peerAddress)
            || !span.TryReadU8(out var advertisingFilterPolicy)
            || !span.TryRead8(out var advertisingTxPower)
            || !span.TryReadU8(out var primaryAdvertisingPhy)
            || !span.TryReadU8(out var secondaryAdvertisingMaxSkip)
            || !span.TryReadU8(out var secondaryAdvertisingPhy)
            || !span.TryReadU8(out var advertisingSid)
            || !span.TryReadU8(out var scanRequestNotificationEnable)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Advertising Handle", FormatHex(advertisingHandle)),
            new("Advertising Event Properties", LeValueFormatter.AdvertisingEventProperties(advertisingEventProperties)),
            new("Primary Advertising Interval Min", LeValueFormatter.Interval625us(primaryAdvertisingIntervalMin)),
            new("Primary Advertising Interval Max", LeValueFormatter.Interval625us(primaryAdvertisingIntervalMax)),
            new("Primary Advertising Channel Map", LeValueFormatter.AdvertisingChannelMap(primaryAdvertisingChannelMap)),
            new("Own Address Type", LeValueFormatter.OwnAddressType(ownAddressType)),
            new("Peer Address Type", LeValueFormatter.PeerAddressType(peerAddressType)),
            new("Peer Address", HciValueFormatter.BdAddr(peerAddress)),
            new("Advertising Filter Policy", LeValueFormatter.AdvertisingFilterPolicy(advertisingFilterPolicy)),
            new("Advertising TX Power", LeValueFormatter.AdvertisingTxPower(advertisingTxPower)),
            new("Primary Advertising PHY", LeValueFormatter.PrimaryAdvertisingPhy(primaryAdvertisingPhy)),
            new("Secondary Advertising Max Skip", FormatHex(secondaryAdvertisingMaxSkip)),
            new("Secondary Advertising PHY", LeValueFormatter.SecondaryAdvertisingPhy(secondaryAdvertisingPhy)),
            new("Advertising SID", LeValueFormatter.AdvertisingSid(advertisingSid)),
            new("Scan Request Notification Enable", LeValueFormatter.ScanRequestNotificationEnable(scanRequestNotificationEnable)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x007F (v2)
    private static DecodedCommand DecodeSetExtendedAdvertisingParametersV2Command(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var advertisingHandle)
            || !span.TryReadU16(out var advertisingEventProperties)
            || !span.TryReadU24(out var primaryAdvertisingIntervalMin)
            || !span.TryReadU24(out var primaryAdvertisingIntervalMax)
            || !span.TryReadU8(out var primaryAdvertisingChannelMap)
            || !span.TryReadU8(out var ownAddressType)
            || !span.TryReadU8(out var peerAddressType)
            || !span.TryReadBytes(6, out var peerAddress)
            || !span.TryReadU8(out var advertisingFilterPolicy)
            || !span.TryRead8(out var advertisingTxPower)
            || !span.TryReadU8(out var primaryAdvertisingPhy)
            || !span.TryReadU8(out var secondaryAdvertisingMaxSkip)
            || !span.TryReadU8(out var secondaryAdvertisingPhy)
            || !span.TryReadU8(out var advertisingSid)
            || !span.TryReadU8(out var scanRequestNotificationEnable)
            || !span.TryReadU8(out var primaryAdvertisingPhyOptions)
            || !span.TryReadU8(out var secondaryAdvertisingPhyOptions)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Advertising Handle", FormatHex(advertisingHandle)),
            new("Advertising Event Properties", LeValueFormatter.AdvertisingEventProperties(advertisingEventProperties)),
            new("Primary Advertising Interval Min", LeValueFormatter.Interval625us(primaryAdvertisingIntervalMin)),
            new("Primary Advertising Interval Max", LeValueFormatter.Interval625us(primaryAdvertisingIntervalMax)),
            new("Primary Advertising Channel Map", LeValueFormatter.AdvertisingChannelMap(primaryAdvertisingChannelMap)),
            new("Own Address Type", LeValueFormatter.OwnAddressType(ownAddressType)),
            new("Peer Address Type", LeValueFormatter.PeerAddressType(peerAddressType)),
            new("Peer Address", HciValueFormatter.BdAddr(peerAddress)),
            new("Advertising Filter Policy", LeValueFormatter.AdvertisingFilterPolicy(advertisingFilterPolicy)),
            new("Advertising TX Power", LeValueFormatter.AdvertisingTxPower(advertisingTxPower)),
            new("Primary Advertising PHY", LeValueFormatter.PrimaryAdvertisingPhy(primaryAdvertisingPhy)),
            new("Secondary Advertising Max Skip", FormatHex(secondaryAdvertisingMaxSkip)),
            new("Secondary Advertising PHY", LeValueFormatter.SecondaryAdvertisingPhy(secondaryAdvertisingPhy)),
            new("Advertising SID", LeValueFormatter.AdvertisingSid(advertisingSid)),
            new("Scan Request Notification Enable", LeValueFormatter.ScanRequestNotificationEnable(scanRequestNotificationEnable)),
            new("Primary Advertising PHY Options", LeValueFormatter.PhyOptions(primaryAdvertisingPhyOptions)),
            new("Secondary Advertising PHY Options", LeValueFormatter.PhyOptions(secondaryAdvertisingPhyOptions)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x0037
    private static DecodedCommand DecodeSetExtendedAdvertisingDataCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var advertisingHandle)
            || !span.TryReadU8(out var operation)
            || !span.TryReadU8(out var fragmentPreference)
            || !span.TryReadU8(out var dataLength)
            || dataLength > 251
            || !span.TryReadBytes(dataLength, out var data)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Advertising Handle", FormatHex(advertisingHandle)),
            new("Operation", LeValueFormatter.Operation(operation)),
            new("Fragment Preference", LeValueFormatter.FragmentPreference(fragmentPreference)),
            new("Advertising Data Length", dataLength.ToString()),
            new("Advertising Data", FormatHexBytes(data)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x0038
    private static DecodedCommand DecodeSetExtendedScanResponseDataCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var advertisingHandle)
            || !span.TryReadU8(out var operation)
            || !span.TryReadU8(out var fragmentPreference)
            || !span.TryReadU8(out var dataLength)
            || dataLength > 251
            || !span.TryReadBytes(dataLength, out var data)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Advertising Handle", FormatHex(advertisingHandle)),
            new("Operation", LeValueFormatter.Operation(operation)),
            new("Fragment Preference", LeValueFormatter.FragmentPreference(fragmentPreference)),
            new("Scan Response Data Length", dataLength.ToString()),
            new("Scan Response Data", FormatHexBytes(data)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x0039
    private static DecodedCommand DecodeSetExtendedAdvertisingEnableCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var enable)
            || !span.TryReadU8(out var numberOfSets))
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Enable", LeValueFormatter.Enable(enable)),
            new("Number Of Sets", numberOfSets.ToString()),
        };

        for (var i = 0; i < numberOfSets; i++)
        {
            if (!span.TryReadU8(out var advertisingHandle)
                || !span.TryReadU16(out var duration)
                || !span.TryReadU8(out var maxExtendedAdvertisingEvents))
            {
                return CreateInvalid(name);
            }

            fields.Add(new HciField($"Set[{i}] Advertising Handle", FormatHex(advertisingHandle)));
            fields.Add(new HciField($"Set[{i}] Duration", FormatHex16(duration)));
            fields.Add(new HciField($"Set[{i}] Max Extended Advertising Events", FormatHex(maxExtendedAdvertisingEvents)));
        }

        if (!span.IsEmpty)
            return CreateInvalid(name);

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x003C
    private static DecodedCommand DecodeRemoveAdvertisingSetCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var advertisingHandle) || !span.IsEmpty)
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Advertising Handle", FormatHex(advertisingHandle)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x0041
    private static DecodedCommand DecodeSetExtendedScanParametersCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var ownAddressType)
            || !span.TryReadU8(out var scanningFilterPolicy)
            || !span.TryReadU8(out var scanningPhys))
        {
            return CreateInvalid(name);
        }

        const byte le1MPhy = 0x01;
        const byte leCodedPhy = 0x04;
        if ((scanningPhys & (le1MPhy | leCodedPhy)) == 0 || (scanningPhys & ~(le1MPhy | leCodedPhy)) != 0)
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Own Address Type", LeValueFormatter.OwnAddressType(ownAddressType)),
            new("Scanning Filter Policy", LeValueFormatter.ScanningFilterPolicy(scanningFilterPolicy)),
            new("Scanning PHYs", LeValueFormatter.ScanningPhys(scanningPhys)),
        };

        if ((scanningPhys & le1MPhy) != 0)
        {
            if (!span.TryReadU8(out var scanType)
                || !span.TryReadU16(out var scanInterval)
                || !span.TryReadU16(out var scanWindow))
            {
                return CreateInvalid(name);
            }

            fields.Add(new HciField("LE 1M Scan Type", LeValueFormatter.ScanType(scanType)));
            fields.Add(new HciField("LE 1M Scan Interval", LeValueFormatter.Interval625us(scanInterval)));
            fields.Add(new HciField("LE 1M Scan Window", LeValueFormatter.Interval625us(scanWindow)));
        }

        if ((scanningPhys & leCodedPhy) != 0)
        {
            if (!span.TryReadU8(out var scanType)
                || !span.TryReadU16(out var scanInterval)
                || !span.TryReadU16(out var scanWindow))
            {
                return CreateInvalid(name);
            }

            fields.Add(new HciField("LE Coded Scan Type", LeValueFormatter.ScanType(scanType)));
            fields.Add(new HciField("LE Coded Scan Interval", LeValueFormatter.Interval625us(scanInterval)));
            fields.Add(new HciField("LE Coded Scan Window", LeValueFormatter.Interval625us(scanWindow)));
        }

        if (!span.IsEmpty)
            return CreateInvalid(name);

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF 0x0042
    private static DecodedCommand DecodeSetExtendedScanEnableCommand(string name, ReadOnlySpan<byte> parameters)
    {
        var span = new HciSpanReader(parameters);
        if (!span.TryReadU8(out var enable)
            || !span.TryReadU8(out var filterDuplicates)
            || !span.TryReadU16(out var duration)
            || !span.TryReadU16(out var period)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Enable", LeValueFormatter.Enable(enable)),
            new("Filter Duplicates", LeValueFormatter.FilterDuplicates(filterDuplicates)),
            new("Duration", FormatScanDuration(duration)),
            new("Period", FormatScanPeriod(period)),
        };

        return new DecodedCommand(name, HciDecodeStatus.Success, fields);
    }

    // OGF 0x08, OCF varies (no-parameter commands)
    private static DecodedCommand DecodeNoParamsCommand(string name, ReadOnlySpan<byte> parameters)
    {
        if (parameters.IsEmpty)
            return new DecodedCommand(name, HciDecodeStatus.Success, Array.Empty<HciField>());

        return CreateInvalid(name);
    }

    private static DecodedCommand CreateInvalid(string name)
        => new(name, HciDecodeStatus.Invalid, Array.Empty<HciField>());

    private static string FormatHex(byte value) => $"0x{value:X2}";
    private static string FormatHex16(ushort value) => $"0x{value:X4}";
    private static string FormatHex24(uint value) => $"0x{value:X6}";

    private static string FormatHexBytes(ReadOnlySpan<byte> value)
        => value.IsEmpty ? "0x" : $"0x{Convert.ToHexString(value)}";

    private static string FormatScanDuration(ushort value)
        => value == 0x0000 ? $"{FormatHex16(value)} (Scan continuously)" : FormatHex16(value);

    private static string FormatScanPeriod(ushort value)
        => value == 0x0000 ? $"{FormatHex16(value)} (Continuous)" : FormatHex16(value);

}
