// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor;

public sealed class UnknownVendorDecoder : IVendorDecoder
{
    public string VendorId => "Vendor Specific";

    public DecodedResult DecodeCommand(HciCommandPacket packet)
        => new(VendorId, HciDecodeStatus.Unknown, Array.Empty<HciField>());

    public DecodedResult DecodeEvent(HciEventPacket packet)
        => new(VendorId, HciDecodeStatus.Unknown, Array.Empty<HciField>());
}
