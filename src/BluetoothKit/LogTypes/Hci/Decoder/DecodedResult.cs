// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BluetoothKit.LogTypes.Hci.Decoder;

public sealed record DecodedResult(string Name, HciDecodeStatus Status, IReadOnlyList<HciField> Fields);
