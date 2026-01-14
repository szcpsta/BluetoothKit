// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BtSnoop.Hci;

namespace BtSnoop.Tests.Hci;

public class HciSpanReaderTests
{
    [Fact]
    public void TryReadBytes_SucceedsAndAdvances()
    {
        var reader = new HciSpanReader(new byte[] { 0xAA, 0xBB, 0xCC });

        var ok = reader.TryReadBytes(2, out var value);

        Assert.True(ok);
        Assert.True(value.SequenceEqual(new byte[] { 0xAA, 0xBB }));
        Assert.Equal(1, reader.Remaining);
        Assert.True(reader.RemainingSpan.SequenceEqual(new byte[] { 0xCC }));
    }

    [Fact]
    public void TryReadBytes_InvalidLength_DoesNotAdvance()
    {
        var reader = new HciSpanReader(new byte[] { 0x10, 0x20 });

        var negative = reader.TryReadBytes(-1, out _);
        var tooLong = reader.TryReadBytes(3, out _);

        Assert.False(negative);
        Assert.False(tooLong);
        Assert.Equal(2, reader.Remaining);
        Assert.True(reader.RemainingSpan.SequenceEqual(new byte[] { 0x10, 0x20 }));
    }

    [Fact]
    public void TrySkip_InvalidLength_DoesNotAdvance()
    {
        var reader = new HciSpanReader(new byte[] { 0x01 });

        var negative = reader.TrySkip(-1);
        var tooLong = reader.TrySkip(2);

        Assert.False(negative);
        Assert.False(tooLong);
        Assert.Equal(1, reader.Remaining);
        Assert.True(reader.RemainingSpan.SequenceEqual(new byte[] { 0x01 }));
    }

    [Fact]
    public void TryReadPrimitives_ReadsLittleEndian()
    {
        byte[] data =
        {
            0x7F,                         // U8
            0x80,                         // 8
            0x34, 0x12,                   // U16
            0xFF, 0x7F,                   // 16
            0x78, 0x56, 0x34, 0x12,       // U32
            0x00, 0x00, 0x00, 0x80,       // 32
            0xEF, 0xCD, 0xAB, 0x89,
            0x67, 0x45, 0x23, 0x01,       // U64
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x80,       // 64
        };

        var reader = new HciSpanReader(data);

        Assert.True(reader.TryReadU8(out var u8));
        Assert.Equal(0x7F, u8);
        Assert.True(reader.TryRead8(out var i8));
        Assert.Equal(unchecked((sbyte)0x80), i8);

        Assert.True(reader.TryReadU16(out var u16));
        Assert.Equal(0x1234, u16);
        Assert.True(reader.TryRead16(out var i16));
        Assert.Equal(0x7FFF, i16);

        Assert.True(reader.TryReadU32(out var u32));
        Assert.Equal(0x12345678u, u32);
        Assert.True(reader.TryRead32(out var i32));
        Assert.Equal(unchecked((int)0x80000000u), i32);

        Assert.True(reader.TryReadU64(out var u64));
        Assert.Equal(0x0123456789ABCDEFul, u64);
        Assert.True(reader.TryRead64(out var i64));
        Assert.Equal(unchecked((long)0x8000000000000000ul), i64);

        Assert.True(reader.IsEmpty);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void TryReadU24_ReadsLittleEndian()
    {
        var reader = new HciSpanReader(new byte[] { 0x01, 0x02, 0x03 });

        var ok = reader.TryReadU24(out var value);

        Assert.True(ok);
        Assert.Equal(0x030201u, value);
        Assert.True(reader.IsEmpty);
    }

    [Fact]
    public void TryReadU48_ReadsLittleEndian()
    {
        var reader = new HciSpanReader(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 });

        var ok = reader.TryReadU48(out var value);

        Assert.True(ok);
        Assert.Equal(0x060504030201ul, value);
        Assert.True(reader.IsEmpty);
    }

    [Fact]
    public void TryReadU16_InsufficientData_DoesNotAdvance()
    {
        var reader = new HciSpanReader(new byte[] { 0xAA });

        var ok = reader.TryReadU16(out _);

        Assert.False(ok);
        Assert.Equal(1, reader.Remaining);
        Assert.True(reader.RemainingSpan.SequenceEqual(new byte[] { 0xAA }));
    }
}
