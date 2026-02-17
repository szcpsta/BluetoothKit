// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;

namespace BluetoothKit.LogTypes.Hci.Decoder.Events;

internal static class LeMetaEventDecoder
{
    internal const byte EventCode = 0x3E;
    internal const string EventName = "LE Meta";

    private delegate DecodedResult DecodeSubeventHandler(string name, byte subeventCode, HciSpanReader span);

    private sealed record SubeventSpec(string Name, DecodeSubeventHandler Decode);

    private static readonly Dictionary<byte, SubeventSpec> Specs = new()
    {
        [0x02] = new("LE Advertising Report", DecodeAdvertisingReportEvent),
        [0x0B] = new("LE Directed Advertising Report", DecodeDirectedAdvertisingReportEvent),
        [0x0D] = new("LE Extended Advertising Report", DecodeExtendedAdvertisingReportEvent),
        [0x11] = new("LE Scan Timeout", DecodeScanTimeoutEvent),
        [0x12] = new("LE Advertising Set Terminated", DecodeAdvertisingSetTerminatedEvent),
        [0x13] = new("LE Scan Request Received", DecodeScanRequestReceivedEvent),
    };

    internal static DecodedResult Decode(HciEventPacket packet)
    {
        var span = new HciSpanReader(packet.Parameters.Span);
        if (!span.TryReadU8(out var subeventCode))
        {
            return new DecodedResult(EventName, HciDecodeStatus.Invalid, Array.Empty<HciField>());
        }

        if (!Specs.TryGetValue(subeventCode, out var spec))
        {
            return new DecodedResult(
                $"{EventName} (Subevent {HciValueFormatter.Hex(subeventCode)})",
                HciDecodeStatus.Unknown,
                Array.Empty<HciField>());
        }

        return spec.Decode(spec.Name, subeventCode, span);
    }

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
