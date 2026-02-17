// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;

namespace BluetoothKit.LogTypes.Hci.Decoder.Commands;

internal static class LeControllerCommandsDecoder
{
    private delegate DecodedResult DecodeCommandHandler(string name, HciSpanReader span);

    private sealed record CommandSpec(string Name, DecodeCommandHandler Decode);

    private static readonly Dictionary<ushort, CommandSpec> Specs = new()
    {
        [0x0006] = new("LE Set Advertising Parameters", DecodeSetAdvertisingParametersCommand),
        [0x0007] = new("LE Read Advertising Physical Channel Tx Power", DecodeNoParamsCommand),
        [0x0008] = new("LE Set Advertising Data", DecodeSetAdvertisingDataCommand),
        [0x0009] = new("LE Set Scan Response Data", DecodeSetScanResponseDataCommand),
        [0x000A] = new("LE Set Advertising Enable", DecodeSetAdvertisingEnableCommand),
        [0x000B] = new("LE Set Scan Parameters", DecodeSetScanParametersCommand),
        [0x000C] = new("LE Set Scan Enable", DecodeSetScanEnableCommand),
        [0x0035] = new("LE Set Advertising Set Random Address", DecodeSetAdvertisingSetRandomAddressCommand),
        [0x0036] = new("LE Set Extended Advertising Parameters [v1]", DecodeSetExtendedAdvertisingParametersV1Command),
        [0x0037] = new("LE Set Extended Advertising Data", DecodeSetExtendedAdvertisingDataCommand),
        [0x0038] = new("LE Set Extended Scan Response Data", DecodeSetExtendedScanResponseDataCommand),
        [0x0039] = new("LE Set Extended Advertising Enable", DecodeSetExtendedAdvertisingEnableCommand),
        [0x003A] = new("LE Read Maximum Advertising Data Length", DecodeNoParamsCommand),
        [0x003B] = new("LE Read Number of Supported Advertising Sets", DecodeNoParamsCommand),
        [0x003C] = new("LE Remove Advertising Set", DecodeRemoveAdvertisingSetCommand),
        [0x003D] = new("LE Clear Advertising Sets", DecodeNoParamsCommand),
        [0x0041] = new("LE Set Extended Scan Parameters", DecodeSetExtendedScanParametersCommand),
        [0x0042] = new("LE Set Extended Scan Enable", DecodeSetExtendedScanEnableCommand),
        [0x007F] = new("LE Set Extended Advertising Parameters [v2]", DecodeSetExtendedAdvertisingParametersV2Command),
    };

    internal static DecodedResult DecodeCommand(HciCommandPacket packet)
    {
        if (!Specs.TryGetValue(packet.Opcode.Ocf, out var spec))
            return new DecodedResult("Unknown", HciDecodeStatus.Unknown, Array.Empty<HciField>());

        return spec.Decode(spec.Name, new HciSpanReader(packet.Parameters.Span));
    }

    private static DecodedResult DecodeSetAdvertisingParametersCommand(string name, HciSpanReader span)
    {
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

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetAdvertisingDataCommand(string name, HciSpanReader span)
    {
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
            new("Advertising Data", HciValueFormatter.HexBytes(payload)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetScanResponseDataCommand(string name, HciSpanReader span)
    {
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
            new("Scan Response Data", HciValueFormatter.HexBytes(payload)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetAdvertisingEnableCommand(string name, HciSpanReader span)
    {
        if (!span.TryReadU8(out var advertisingEnable) || !span.IsEmpty)
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Advertising Enable", LeValueFormatter.Enable(advertisingEnable)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetScanParametersCommand(string name, HciSpanReader span)
    {
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

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetScanEnableCommand(string name, HciSpanReader span)
    {
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

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetAdvertisingSetRandomAddressCommand(string name, HciSpanReader span)
    {
        if (!span.TryReadU8(out var advertisingHandle)
            || !span.TryReadBytes(6, out var randomAddress)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Advertising Handle", HciValueFormatter.Hex(advertisingHandle)),
            new("Random Address", HciValueFormatter.BdAddr(randomAddress)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetExtendedAdvertisingParametersV1Command(string name, HciSpanReader span)
    {
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
            new("Advertising Handle", HciValueFormatter.Hex(advertisingHandle)),
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
            new("Secondary Advertising Max Skip", HciValueFormatter.Hex(secondaryAdvertisingMaxSkip)),
            new("Secondary Advertising PHY", LeValueFormatter.SecondaryAdvertisingPhy(secondaryAdvertisingPhy)),
            new("Advertising SID", LeValueFormatter.AdvertisingSid(advertisingSid)),
            new("Scan Request Notification Enable", LeValueFormatter.ScanRequestNotificationEnable(scanRequestNotificationEnable)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetExtendedAdvertisingParametersV2Command(string name, HciSpanReader span)
    {
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
            new("Advertising Handle", HciValueFormatter.Hex(advertisingHandle)),
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
            new("Secondary Advertising Max Skip", HciValueFormatter.Hex(secondaryAdvertisingMaxSkip)),
            new("Secondary Advertising PHY", LeValueFormatter.SecondaryAdvertisingPhy(secondaryAdvertisingPhy)),
            new("Advertising SID", LeValueFormatter.AdvertisingSid(advertisingSid)),
            new("Scan Request Notification Enable", LeValueFormatter.ScanRequestNotificationEnable(scanRequestNotificationEnable)),
            new("Primary Advertising PHY Options", LeValueFormatter.PhyOptions(primaryAdvertisingPhyOptions)),
            new("Secondary Advertising PHY Options", LeValueFormatter.PhyOptions(secondaryAdvertisingPhyOptions)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetExtendedAdvertisingDataCommand(string name, HciSpanReader span)
    {
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
            new("Advertising Handle", HciValueFormatter.Hex(advertisingHandle)),
            new("Operation", LeValueFormatter.Operation(operation)),
            new("Fragment Preference", LeValueFormatter.FragmentPreference(fragmentPreference)),
            new("Advertising Data Length", dataLength.ToString()),
            new("Advertising Data", HciValueFormatter.HexBytes(data)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetExtendedScanResponseDataCommand(string name, HciSpanReader span)
    {
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
            new("Advertising Handle", HciValueFormatter.Hex(advertisingHandle)),
            new("Operation", LeValueFormatter.Operation(operation)),
            new("Fragment Preference", LeValueFormatter.FragmentPreference(fragmentPreference)),
            new("Scan Response Data Length", dataLength.ToString()),
            new("Scan Response Data", HciValueFormatter.HexBytes(data)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetExtendedAdvertisingEnableCommand(string name, HciSpanReader span)
    {
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

            fields.Add(new HciField($"Set[{i}] Advertising Handle", HciValueFormatter.Hex(advertisingHandle)));
            fields.Add(new HciField($"Set[{i}] Duration", HciValueFormatter.Hex16(duration)));
            fields.Add(new HciField($"Set[{i}] Max Extended Advertising Events", HciValueFormatter.Hex(maxExtendedAdvertisingEvents)));
        }

        if (!span.IsEmpty)
            return CreateInvalid(name);

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeRemoveAdvertisingSetCommand(string name, HciSpanReader span)
    {
        if (!span.TryReadU8(out var advertisingHandle) || !span.IsEmpty)
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Advertising Handle", HciValueFormatter.Hex(advertisingHandle)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetExtendedScanParametersCommand(string name, HciSpanReader span)
    {
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

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeSetExtendedScanEnableCommand(string name, HciSpanReader span)
    {
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
            new("Duration", LeValueFormatter.ScanDuration(duration)),
            new("Period", LeValueFormatter.ScanPeriod(period)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeNoParamsCommand(string name, HciSpanReader span)
    {
        if (span.IsEmpty)
            return new DecodedResult(name, HciDecodeStatus.Success, Array.Empty<HciField>());

        return CreateInvalid(name);
    }

    private static DecodedResult CreateInvalid(string name)
        => new(name, HciDecodeStatus.Invalid, Array.Empty<HciField>());

}
