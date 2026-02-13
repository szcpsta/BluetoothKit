// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Hci.Reader;

namespace BluetoothKit.Tests.LogTypes.Hci.Reader;

public class BtSnoopReaderTests
{
    private readonly byte[] _btSnoop =
    {
        0x62, 0x74, 0x73, 0x6e, 0x6f, 0x6f, 0x70, 0x00,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x03, 0xea,
        0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04,
        0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00,
        0x00, 0xe3, 0x19, 0x96, 0x64, 0x5e, 0x25, 0x20,
        0x01, 0x03, 0x0c, 0x00, 0x00, 0x00, 0x00, 0x07,
        0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x03,
        0x00, 0x00, 0x00, 0x00, 0x00, 0xe3, 0x19, 0x96,
        0x64, 0x5e, 0x41, 0x17, 0x04, 0x0e, 0x04, 0x01,
        0x03, 0x0c, 0x00, 0x00, 0x00, 0x00, 0x0c, 0x00,
        0x00, 0x00, 0x0c, 0x00, 0x00, 0x00, 0x02, 0x00,
        0x00, 0x00, 0x00, 0x00, 0xe3, 0x19, 0x96, 0x64,
        0x5e, 0x41, 0x8a, 0x01, 0x01, 0x0c, 0x08, 0xff,
        0xff, 0xff, 0xff, 0xff, 0xff, 0xbf, 0x3d,
    };

    [Fact]
    public void ArgumentNullExceptionTest_ForStream()
    {
        Assert.Throws<ArgumentNullException>(() => new BtSnoopReader(stream: null!));
    }

    [Fact]
    public void Dispose_ShouldCloseStream_WhenLeaveOpenIsFalse()
    {
        var stream = new MemoryStream(_btSnoop);
        var reader = new BtSnoopReader(stream, leaveOpen: false);

        reader.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }

    [Fact]
    public void Dispose_ShouldNotCloseStream_WhenLeaveOpenIsTrue()
    {
        var stream = new MemoryStream(_btSnoop);
        var reader = new BtSnoopReader(stream, leaveOpen: true);

        reader.Dispose();

        Assert.Equal(0x62, stream.ReadByte()); // 'b'
        stream.Dispose();
    }

    [Fact]
    public async Task PacketCountAndTimestampTest()
    {
        List<BtSnoopReader.BtSnoopRecord> snoopRecords = [];

        var stream = new MemoryStream(_btSnoop);
        var reader = new BtSnoopReader(stream, leaveOpen: true);
        await foreach (var record in reader.ReadAsync())
        {
            snoopRecords.Add(record);
        }

        Assert.Equal(3, snoopRecords.Count);
        Assert.Equal(new DateTime(2025, 8, 8, 23, 57, 12, 999, 200, DateTimeKind.Utc),
            snoopRecords[0].GetDateTimeUtc());
        Assert.Equal(new DateTime(2025, 8, 8, 23, 57, 13, 006, 359, DateTimeKind.Utc),
            snoopRecords[1].GetDateTimeUtc());
        Assert.Equal(new DateTime(2025, 8, 8, 23, 57, 13, 006, 474, DateTimeKind.Utc),
            snoopRecords[2].GetDateTimeUtc());

        Assert.Equal(stream.Length, stream.Position);

        await reader.DisposeAsync();
        await stream.DisposeAsync();
    }
}
