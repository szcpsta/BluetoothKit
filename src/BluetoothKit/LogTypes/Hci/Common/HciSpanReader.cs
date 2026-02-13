// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace BluetoothKit.LogTypes.Hci.Common;

internal ref struct HciSpanReader
{
    private ReadOnlySpan<byte> _span;

    public HciSpanReader(ReadOnlySpan<byte> span) => _span = span;

    public int Remaining => _span.Length;
    public bool IsEmpty => _span.IsEmpty;
    public ReadOnlySpan<byte> RemainingSpan => _span;

    // ----------------------------
    // Guards / helpers
    // ----------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryEnsure(int byteCount) => byteCount >= 0 && _span.Length >= byteCount;

    // ----------------------------
    // Core consume operations
    // ----------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBytes(int length, out ReadOnlySpan<byte> value)
    {
        if (!TryEnsure(length))
        {
            value = default;
            return false;
        }

        value = _span[..length];
        _span = _span[length..];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySkip(int byteCount)
    {
        if (!TryEnsure(byteCount))
            return false;

        _span = _span[byteCount..];
        return true;
    }

    // ----------------------------
    // Primitive reads (HCI: little-endian by default)
    // ----------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadU8(out byte value)
    {
        if (!TryEnsure(1))
        {
            value = default;
            return false;
        }

        value = _span[0];
        _span = _span[1..];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead8(out sbyte value)
    {
        if (!TryReadU8(out var v))
        {
            value = default;
            return false;
        }

        value = unchecked((sbyte)v);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadU16(out ushort value)
    {
        if (!TryEnsure(2))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt16LittleEndian(_span);
        _span = _span[2..];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead16(out short value)
    {
        if (!TryReadU16(out var v))
        {
            value = default;
            return false;
        }

        value = unchecked((short)v);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadU32(out uint value)
    {
        if (!TryEnsure(4))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32LittleEndian(_span);
        _span = _span[4..];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead32(out int value)
    {
        if (!TryReadU32(out var v))
        {
            value = default;
            return false;
        }

        value = unchecked((int)v);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadU64(out ulong value)
    {
        if (!TryEnsure(8))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64LittleEndian(_span);
        _span = _span[8..];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead64(out long value)
    {
        if (!TryReadU64(out var v))
        {
            value = default;
            return false;
        }

        value = unchecked((long)v);
        return true;
    }

    // ----------------------------
    // HCI convenience helpers
    // ----------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadU24(out uint value)
    {
        if (!TryEnsure(3))
        {
            value = default;
            return false;
        }

        uint b0 = _span[0];
        uint b1 = _span[1];
        uint b2 = _span[2];
        _span = _span[3..];

        // Little-endian in buffer: b0 is LSB.
        value = (b0) | (b1 << 8) | (b2 << 16);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadU48(out ulong value)
    {
        if (!TryEnsure(6))
        {
            value = default;
            return false;
        }

        ulong b0 = _span[0];
        ulong b1 = _span[1];
        ulong b2 = _span[2];
        ulong b3 = _span[3];
        ulong b4 = _span[4];
        ulong b5 = _span[5];
        _span = _span[6..];

        // Little-endian in buffer: b0 is LSB.
        value = (b0) | (b1 << 8) | (b2 << 16) | (b3 << 24) | (b4 << 32) | (b5 << 40);
        return true;
    }
}
