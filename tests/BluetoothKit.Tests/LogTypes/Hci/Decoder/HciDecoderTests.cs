// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Common;
using BluetoothKit.LogTypes.Hci.Decoder;
using BluetoothKit.LogTypes.Hci.Decoder.Vendor.Samsung;
using BluetoothKit.LogTypes.Hci.Parser;
using BluetoothKit.LogTypes.Hci.Reader;

namespace BluetoothKit.Tests.LogTypes.Hci.Decoder;

public class HciDecoderTests
{
    private const string BtSnoopRelativePath = "btsnoop_hci.log";

    [LogFileExistsFact(BtSnoopRelativePath)]
    public async Task VendorPacketTest()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", BtSnoopRelativePath);
        await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var reader = new BtSnoopReader(stream, leaveOpen: false);

        int vscCount = 0;
        int vseCount = 0;

        var decoder = new HciDecoder();

        await foreach (var record in reader.ReadAsync())
        {
            if (HciPacketParser.TryParse(record.PacketData, out var packet))
            {
                HciDecodedPacket decoded = decoder.Decode(packet);

                if (packet is HciCommandPacket cmd && cmd.Opcode.IsVendorSpecific)
                {
                    var decodedCommand = decoded as HciDecodedCommand;

                    Assert.NotNull(decodedCommand);
                    Assert.Equal("Vendor Specific", decodedCommand.Name);
                    vscCount++;
                }
                else if (packet is HciEventPacket evt && evt.EventCode.IsVendorSpecific)
                {
                    var decodedEvent = decoded as HciDecodedEvent;

                    Assert.NotNull(decodedEvent);
                    Assert.Equal("Vendor Specific", decodedEvent.Name);
                    vseCount++;
                }
            }
        }

        Assert.Equal(78, vscCount);
        Assert.Equal(1, vseCount);
    }
}

public class LogFileExistsFactAttribute : FactAttribute
{
    public LogFileExistsFactAttribute(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", relativePath);
        if (!File.Exists(path))
            Skip = $"Skipping: Log file '{path}' not found.";
    }
}
