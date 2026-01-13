// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BtSnoop.Reader;

namespace BtSnoop.Tests.Reader;

public class BtSnoopReaderTests
{
    [Fact]
    public void ArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => new BtSnoopReader(null));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var reader = new BtSnoopReader(new MemoryStream());

        reader.Dispose();
        reader.Dispose(); // No exception expected
    }

    [Fact]
    public void Dispose_ShouldCloseStream_WhenLeaveOpenIsFalse()
    {
        var ms = new MemoryStream();
        var reader = new BtSnoopReader(ms, leaveOpen: false);

        reader.Dispose();

        Assert.Throws<ObjectDisposedException>(() => ms.Position);
    }

    [Fact]
    public void Dispose_ShouldNotCloseStream_WhenLeaveOpenIsTrue()
    {
        var ms = new MemoryStream();
        var reader = new BtSnoopReader(ms, leaveOpen: true);

        reader.Dispose();

        // Should still work
        ms.Position = 0;
    }

    [Fact]
    public async Task PacketCountAndTimestampTest()
    {
        byte[] btSnoop =
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

        List<BtSnoopReader.BtSnoopRecord> snoopRecords = [];

        var reader = new BtSnoopReader(new MemoryStream(btSnoop));
        long lastReport = -1;
        var progress = new Progress<long>(v => lastReport = v);
        await foreach (var record in reader.ReadAsync(progress))
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

        Assert.Equal(btSnoop.Length, lastReport);
    }
}
