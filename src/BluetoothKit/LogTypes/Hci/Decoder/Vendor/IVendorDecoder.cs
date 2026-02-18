// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.LogTypes.Hci.Decoder.Vendor;

public interface IVendorDecoder
{
    string VendorId { get; }

    HciDecodedCommand DecodeCommand(HciCommandPacket packet);
    HciDecodedEvent DecodeEvent(HciEventPacket packet);
}
