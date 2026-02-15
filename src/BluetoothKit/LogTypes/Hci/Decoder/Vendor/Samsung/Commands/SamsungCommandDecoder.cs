// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung.Commands;

internal sealed class SamsungCommandDecoder
{
    public bool TryDecodeCommand(HciCommandPacket packet, out DecodedResult decoded)
    {
        decoded = default!;

        if (!packet.Opcode.IsVendorSpecific)
            return false;

        switch (packet.Opcode.Ocf)
        {
            default:
                return false;
        }
    }
}
