// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor;

public sealed class UnknownVendorDecoder : IVendorDecoder
{
    public string VendorId => "Vendor Specific";

    public bool TryDecodeCommand(HciCommandPacket packet, out DecodedResult decoded)
    {
        decoded = default!;
        return false;
    }

    public bool TryDecodeEvent(HciEventPacket packet, out DecodedResult decoded)
    {
        decoded = default!;
        return false;
    }
}
