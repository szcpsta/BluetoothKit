// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung.Commands;

internal sealed class SamsungCommandDecoder
{
    public DecodedResult DecodeCommand(HciCommandPacket packet)
    {
        return new DecodedResult(VendorIds.Samsung, HciDecodeStatus.Unknown, Array.Empty<HciField>());
    }
}
