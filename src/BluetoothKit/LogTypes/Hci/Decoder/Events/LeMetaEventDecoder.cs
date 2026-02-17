// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;

namespace BluetoothKit.LogTypes.Hci.Decoder.Events;

internal static class LeMetaEventDecoder
{
    internal static bool TryDecodeEvent(HciEventPacket packet, out DecodedResult decoded)
    {
        decoded = default!;

        if (packet.EventCode.Value != 0x3E)
            return false;

        var span = new HciSpanReader(packet.Parameters.Span);
        if (!span.TryReadU8(out var subeventCode))
        {
            decoded = CreateInvalid("LE Meta event");
            return true;
        }

        switch (subeventCode)
        {
            case 0x02:
                decoded = DecodeAdvertisingReportEvent("LE Advertising Report", subeventCode, span);
                return true;
            case 0x0B:
                decoded = DecodeDirectedAdvertisingReportEvent("LE Directed Advertising Report", subeventCode, span);
                return true;
            case 0x0D:
                decoded = DecodeExtendedAdvertisingReportEvent("LE Extended Advertising Report", subeventCode, span);
                return true;
            case 0x11:
                decoded = DecodeScanTimeoutEvent("LE Scan Timeout", subeventCode, span);
                return true;
            case 0x12:
                decoded = DecodeAdvertisingSetTerminatedEvent("LE Advertising Set Terminated", subeventCode, span);
                return true;
            case 0x13:
                decoded = DecodeScanRequestReceivedEvent("LE Scan Request Received", subeventCode, span);
                return true;
            default:
                decoded = new DecodedResult($"LE Meta event (Subevent {HciValueFormatter.Hex(subeventCode)})", HciDecodeStatus.Unknown, Array.Empty<HciField>());
                return true;
        }
    }

    // Event Code 0x3E, Subevent 0x02
    private static DecodedResult DecodeAdvertisingReportEvent(string name, byte subeventCode, HciSpanReader span)
    {
        if (!span.TryReadU8(out var numReports))
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Subevent Code", HciValueFormatter.Hex(subeventCode)),
            new("Num Reports", numReports.ToString()),
        };

        for (var i = 0; i < numReports; i++)
        {
            if (!span.TryReadU8(out var eventType)
                || !span.TryReadU8(out var addressType)
                || !span.TryReadBytes(6, out var address)
                || !span.TryReadU8(out var dataLength)
                || !span.TryReadBytes(dataLength, out var data)
                || !span.TryRead8(out var rssi))
            {
                return CreateInvalid(name);
            }

            fields.Add(new HciField($"Report[{i}] Event Type", LeValueFormatter.LegacyAdvertisingEventType(eventType)));
            fields.Add(new HciField($"Report[{i}] Address Type", LeValueFormatter.AddressType(addressType)));
            fields.Add(new HciField($"Report[{i}] Address", HciValueFormatter.BdAddr(address)));
            fields.Add(new HciField($"Report[{i}] Data Length", dataLength.ToString()));
            fields.Add(new HciField($"Report[{i}] Data", HciValueFormatter.HexBytes(data)));
            fields.Add(new HciField($"Report[{i}] RSSI", LeValueFormatter.Dbm(rssi)));
        }

        if (!span.IsEmpty)
            return CreateInvalid(name);

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    // Event Code 0x3E, Subevent 0x0B
    private static DecodedResult DecodeDirectedAdvertisingReportEvent(string name, byte subeventCode, HciSpanReader span)
    {
        if (!span.TryReadU8(out var numReports))
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Subevent Code", HciValueFormatter.Hex(subeventCode)),
            new("Num Reports", numReports.ToString()),
        };

        for (var i = 0; i < numReports; i++)
        {
            if (!span.TryReadU8(out var eventType)
                || !span.TryReadU8(out var addressType)
                || !span.TryReadBytes(6, out var address)
                || !span.TryReadU8(out var directAddressType)
                || !span.TryReadBytes(6, out var directAddress)
                || !span.TryRead8(out var rssi))
            {
                return CreateInvalid(name);
            }

            fields.Add(new HciField($"Report[{i}] Event Type", LeValueFormatter.LegacyAdvertisingEventType(eventType)));
            fields.Add(new HciField($"Report[{i}] Address Type", LeValueFormatter.AddressType(addressType)));
            fields.Add(new HciField($"Report[{i}] Address", HciValueFormatter.BdAddr(address)));
            fields.Add(new HciField($"Report[{i}] Direct Address Type", LeValueFormatter.AddressType(directAddressType)));
            fields.Add(new HciField($"Report[{i}] Direct Address", HciValueFormatter.BdAddr(directAddress)));
            fields.Add(new HciField($"Report[{i}] RSSI", LeValueFormatter.Dbm(rssi)));
        }

        if (!span.IsEmpty)
            return CreateInvalid(name);

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    // Event Code 0x3E, Subevent 0x0D
    private static DecodedResult DecodeExtendedAdvertisingReportEvent(string name, byte subeventCode, HciSpanReader span)
    {
        if (!span.TryReadU8(out var numReports))
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Subevent Code", HciValueFormatter.Hex(subeventCode)),
            new("Num Reports", numReports.ToString()),
        };

        for (var i = 0; i < numReports; i++)
        {
            if (!span.TryReadU16(out var eventType)
                || !span.TryReadU8(out var addressType)
                || !span.TryReadBytes(6, out var address)
                || !span.TryReadU8(out var primaryPhy)
                || !span.TryReadU8(out var secondaryPhy)
                || !span.TryReadU8(out var advertisingSid)
                || !span.TryRead8(out var txPower)
                || !span.TryRead8(out var rssi)
                || !span.TryReadU16(out var periodicAdvertisingInterval)
                || !span.TryReadU8(out var directAddressType)
                || !span.TryReadBytes(6, out var directAddress)
                || !span.TryReadU8(out var dataLength)
                || !span.TryReadBytes(dataLength, out var data))
            {
                return CreateInvalid(name);
            }

            fields.Add(new HciField($"Report[{i}] Event Type", LeValueFormatter.ExtendedAdvertisingEventType(eventType)));
            fields.Add(new HciField($"Report[{i}] Address Type", LeValueFormatter.AddressType(addressType)));
            fields.Add(new HciField($"Report[{i}] Address", HciValueFormatter.BdAddr(address)));
            fields.Add(new HciField($"Report[{i}] Primary PHY", LeValueFormatter.PrimaryAdvertisingPhy(primaryPhy)));
            fields.Add(new HciField($"Report[{i}] Secondary PHY", LeValueFormatter.SecondaryAdvertisingPhy(secondaryPhy)));
            fields.Add(new HciField($"Report[{i}] Advertising SID", LeValueFormatter.AdvertisingSid(advertisingSid)));
            fields.Add(new HciField($"Report[{i}] TX Power", LeValueFormatter.Dbm(txPower)));
            fields.Add(new HciField($"Report[{i}] RSSI", LeValueFormatter.Dbm(rssi)));
            fields.Add(new HciField($"Report[{i}] Periodic Advertising Interval", LeValueFormatter.PeriodicAdvertisingInterval(periodicAdvertisingInterval)));
            fields.Add(new HciField($"Report[{i}] Direct Address Type", LeValueFormatter.DirectAddressType(directAddressType)));
            fields.Add(new HciField($"Report[{i}] Direct Address", HciValueFormatter.BdAddr(directAddress)));
            fields.Add(new HciField($"Report[{i}] Data Length", dataLength.ToString()));
            fields.Add(new HciField($"Report[{i}] Data", HciValueFormatter.HexBytes(data)));
        }

        if (!span.IsEmpty)
            return CreateInvalid(name);

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    // Event Code 0x3E, Subevent 0x11
    private static DecodedResult DecodeScanTimeoutEvent(string name, byte subeventCode, HciSpanReader span)
    {
        if (!span.IsEmpty)
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Subevent Code", HciValueFormatter.Hex(subeventCode)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    // Event Code 0x3E, Subevent 0x12
    private static DecodedResult DecodeAdvertisingSetTerminatedEvent(string name, byte subeventCode, HciSpanReader span)
    {
        if (!span.TryReadU8(out var status)
            || !span.TryReadU8(out var advertisingHandle)
            || !span.TryReadU16(out var connectionHandle)
            || !span.TryReadU8(out var numCompletedEvents)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Subevent Code", HciValueFormatter.Hex(subeventCode)),
            new("Status", HciValueFormatter.Hex(status)),
            new("Advertising Handle", HciValueFormatter.Hex(advertisingHandle)),
            new("Connection Handle", HciValueFormatter.Hex16(connectionHandle)),
            new("Num Completed Extended Advertising Events", HciValueFormatter.Hex(numCompletedEvents)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    // Event Code 0x3E, Subevent 0x13
    private static DecodedResult DecodeScanRequestReceivedEvent(string name, byte subeventCode, HciSpanReader span)
    {
        if (!span.TryReadU8(out var advertisingHandle)
            || !span.TryReadU8(out var scannerAddressType)
            || !span.TryReadBytes(6, out var scannerAddress)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Subevent Code", HciValueFormatter.Hex(subeventCode)),
            new("Advertising Handle", HciValueFormatter.Hex(advertisingHandle)),
            new("Scanner Address Type", LeValueFormatter.AddressType(scannerAddressType)),
            new("Scanner Address", HciValueFormatter.BdAddr(scannerAddress)),
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult CreateInvalid(string name)
        => new(name, HciDecodeStatus.Invalid, Array.Empty<HciField>());

}
