// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Parser;
using BluetoothKit.LogTypes.Hci.Reader;

namespace BluetoothKit.Tests.LogTypes.Hci;

public class HciTests
{
    private const string BtSnoopRelativePath = "btsnoop_hci.log";

    [LogFileExistsFact(BtSnoopRelativePath)]
    public async Task CountPacketTypes()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", BtSnoopRelativePath);
        await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var reader = new BtSnoopReader(stream, leaveOpen: false);

        int totalCount = 0;
        var packetTypeCounts = new Dictionary<byte, int>();

        await foreach (var record in reader.ReadAsync())
        {
            totalCount++;

            _ = HciPacketParser.TryParse(record.PacketData, out var packet);
            byte typeValue = packet.PacketType.Value;

            packetTypeCounts[typeValue] = packetTypeCounts.TryGetValue(typeValue, out var count)
                ? count + 1
                : 1;
        }

        Assert.Equal(4846, totalCount);

        Assert.Equal(381, packetTypeCounts.GetValueOrDefault<byte, int>(1, 0));
        Assert.Equal(2982, packetTypeCounts.GetValueOrDefault<byte, int>(2, 0));
        Assert.Equal(0, packetTypeCounts.GetValueOrDefault<byte, int>(3, 0));
        Assert.Equal(1483, packetTypeCounts.GetValueOrDefault<byte, int>(4, 0));
        Assert.Equal(0, packetTypeCounts.GetValueOrDefault<byte, int>(5, 0));
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
