// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor;

public sealed class UnknownVendorDecoder : IVendorDecoder
{
    public string VendorId => "Vendor Specific";

    public HciDecodedCommand DecodeCommand(HciCommandPacket packet)
        => new(packet, HciDecodeStatus.Unknown, VendorId, Array.Empty<HciField>());

    public HciDecodedEvent DecodeEvent(HciEventPacket packet)
        => new(packet, HciDecodeStatus.Unknown, VendorId, Array.Empty<HciField>());
}
