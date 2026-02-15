// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Vendor;

namespace BluetoothKit.LogTypes.Hci.Decoder.Events;

public class HciEventDecoder
{
    private readonly IVendorDecoder _vendorDecoder;

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
            if (_vendorDecoder.TryDecodeEvent(packet, out var vendorDecoded))
                return new HciDecodedEvent(packet, vendorDecoded.Status, vendorDecoded.Name, vendorDecoded.Fields);

            return new HciDecodedEvent(packet, HciDecodeStatus.Unknown, _vendorDecoder.VendorId, Array.Empty<HciField>());
        }

        return packet.EventCode.Value switch
        {
            0x3E when LeMetaEventDecoder.TryDecodeEvent(packet, out var leDecoded)
                => new HciDecodedEvent(packet, leDecoded.Status, leDecoded.Name, leDecoded.Fields),
            0x0E => DecodeCommandCompleteEvent("Command Complete", packet),
            0x0F => DecodeCommandStatusEvent("Command Status", packet),
            _ => new HciDecodedEvent(packet, HciDecodeStatus.Unknown, "Unknown", Array.Empty<HciField>())
        };
    }

    // Event Code 0x0E
    private static HciDecodedEvent DecodeCommandCompleteEvent(string name, HciEventPacket packet)
    {
        var span = new HciSpanReader(packet.Parameters.Span);
        if (!span.TryReadU8(out var numHciCommandPackets)
            || !span.TryReadU16(out var opcodeValue))
        {
            return CreateInvalid(packet, name);
        }

        var opcode = new HciOpcode(opcodeValue);
        if (!span.TryReadU8(out var status))
            return CreateInvalid(packet, name);

        var fields = new List<HciField>
        {
            new("Num HCI Command Packets", numHciCommandPackets.ToString()),
            new("Opcode", opcode.ToString()),
            new("Status", status == 0 ? "0x00" : $"0x{status:X2}")
        };

        if (!span.IsEmpty)
            fields.Add(new HciField("Return Parameters", Convert.ToHexString(span.RemainingSpan)));

        return new HciDecodedEvent(packet, HciDecodeStatus.Success, name, fields);
    }

    // Event Code 0x0F
    private static HciDecodedEvent DecodeCommandStatusEvent(string name, HciEventPacket packet)
    {
        var span = new HciSpanReader(packet.Parameters.Span);
        if (!span.TryReadU8(out var status)
            || !span.TryReadU8(out var numHciCommandPackets)
            || !span.TryReadU16(out var opcodeValue)
            || !span.IsEmpty)
        {
            return CreateInvalid(packet, name);
        }

        var fields = new List<HciField>
        {
            new("Status", status == 0 ? "0x00" : $"0x{status:X2}"),
            new("Num HCI Command Packets", numHciCommandPackets.ToString()),
            new("Opcode", new HciOpcode(opcodeValue).ToString())
        };

        return new HciDecodedEvent(packet, HciDecodeStatus.Success, name, fields);
    }

    private static HciDecodedEvent CreateInvalid(HciEventPacket packet, string name)
        => new(packet, HciDecodeStatus.Invalid, name, Array.Empty<HciField>());
}
