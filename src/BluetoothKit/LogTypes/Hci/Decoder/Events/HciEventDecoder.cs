// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Formatters;
using BluetoothKit.LogTypes.Hci.Decoder.Vendor;

namespace BluetoothKit.LogTypes.Hci.Decoder.Events;

public class HciEventDecoder
{
    private readonly IVendorDecoder _vendorDecoder;

    private delegate DecodedResult DecodeHandler(string name, HciSpanReader span);

    private sealed record EventSpec(string Name, DecodeHandler Decode);

    private static readonly Dictionary<byte, EventSpec> EventSpecs = new()
    {
        [0x0E] = new("Command Complete", DecodeCommandCompleteEvent),
        [0x0F] = new("Command Status", DecodeCommandStatusEvent),
    };

    public HciEventDecoder() : this(new UnknownVendorDecoder())
    {
    }

    internal HciEventDecoder(IVendorDecoder vendorDecoder)
    {
        _vendorDecoder = vendorDecoder;
    }

    public HciDecodedEvent Decode(HciEventPacket packet)
    {
        if (packet.EventCode.IsVendorSpecific)
        {
            var vendorDecoded = _vendorDecoder.DecodeEvent(packet);
            return new HciDecodedEvent(packet, vendorDecoded.Status, vendorDecoded.Name, vendorDecoded.Fields);
        }

        if (packet.EventCode.Value == LeMetaEventDecoder.EventCode)
        {
            var leDecoded = LeMetaEventDecoder.Decode(packet);
            return new HciDecodedEvent(packet, leDecoded.Status, leDecoded.Name, leDecoded.Fields);
        }

        if (EventSpecs.TryGetValue(packet.EventCode.Value, out var spec))
        {
            var span = new HciSpanReader(packet.Parameters.Span);
            var decoded = spec.Decode(spec.Name, span);
            return new HciDecodedEvent(packet, decoded.Status, decoded.Name, decoded.Fields);
        }

        return new HciDecodedEvent(packet, HciDecodeStatus.Unknown, "Unknown", Array.Empty<HciField>());
    }

    private static DecodedResult DecodeCommandCompleteEvent(string name, HciSpanReader span)
    {
        if (!span.TryReadU8(out var numHciCommandPackets)
            || !span.TryReadU16(out var opcodeValue))
        {
            return CreateInvalid(name);
        }

        var opcode = new HciOpcode(opcodeValue);
        if (!span.TryReadU8(out var status))
            return CreateInvalid(name);

        var fields = new List<HciField>
        {
            new("Num HCI Command Packets", numHciCommandPackets.ToString()),
            new("Opcode", opcode.ToString()),
            new("Status", HciValueFormatter.Hex(status))
        };

        if (!span.IsEmpty)
            fields.Add(new HciField("Return Parameters", HciValueFormatter.HexBytes(span.RemainingSpan)));

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult DecodeCommandStatusEvent(string name, HciSpanReader span)
    {
        if (!span.TryReadU8(out var status)
            || !span.TryReadU8(out var numHciCommandPackets)
            || !span.TryReadU16(out var opcodeValue)
            || !span.IsEmpty)
        {
            return CreateInvalid(name);
        }

        var fields = new List<HciField>
        {
            new("Status", HciValueFormatter.Hex(status)),
            new("Num HCI Command Packets", numHciCommandPackets.ToString()),
            new("Opcode", new HciOpcode(opcodeValue).ToString())
        };

        return new DecodedResult(name, HciDecodeStatus.Success, fields);
    }

    private static DecodedResult CreateInvalid(string name)
        => new(name, HciDecodeStatus.Invalid, Array.Empty<HciField>());
}
