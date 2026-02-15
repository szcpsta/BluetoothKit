// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung.Events;

internal sealed class SamsungEventDecoder
{
    public bool TryDecodeEvent(HciEventPacket packet, out DecodedResult decoded)
    {
        decoded = default!;

        if (!packet.EventCode.IsVendorSpecific)
            return false;

        return false;
    }
}
