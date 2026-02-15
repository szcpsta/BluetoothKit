// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung.Commands;
using BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung.Events;

namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung;

public sealed class SamsungVendorDecoder : IVendorDecoder
{
    private readonly SamsungCommandDecoder _commandDecoder = new();
    private readonly SamsungEventDecoder _eventDecoder = new();

    public string VendorId => VendorIds.Samsung;

    public bool TryDecodeCommand(HciCommandPacket packet, out DecodedResult decoded)
        => _commandDecoder.TryDecodeCommand(packet, out decoded);

    public bool TryDecodeEvent(HciEventPacket packet, out DecodedResult decoded)
        => _eventDecoder.TryDecodeEvent(packet, out decoded);
}
