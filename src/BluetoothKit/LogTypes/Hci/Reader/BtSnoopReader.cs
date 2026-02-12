// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Text;

namespace BluetoothKit.LogTypes.Hci.Reader;

public sealed class BtSnoopReader : IDisposable, IAsyncDisposable
{
    private const string IdentificationPattern = "btsnoop\0";
    private const uint VersionNumber = 1;

    private enum FileHeaderOffset
    {
        IdentificationPattern = 0,                          // 0
        VersionNumber = IdentificationPattern + 8,          // 8
        DatalinkType = VersionNumber + 4,                   // 12
        FileHeaderLength = DatalinkType + 4,                // 16
    }

    private enum PacketRecordOffset
    {
        OriginalLength = 0,                                 // 0
        IncludedLength = OriginalLength + 4,                // 4
        PacketFlag = IncludedLength + 4,                    // 8
        CumulativeDrops = PacketFlag + 4,                   // 12
        TimestampMicroseconds = CumulativeDrops + 4,        // 16
        HeaderLength = TimestampMicroseconds + 8,           // 24
    }

    private enum DatalinkCode
    {
        H1 = 1001,
        H4 = 1002,
        Bscp = 1003,
        H5 = 1004,
    }

    [Flags]
    private enum PacketFlagBit
    {
        Direction = 1 << 0,  // Direction flag 0 = Sent, 1 = Received
        Command = 1 << 1,    // Command flag 0 = Data, 1 = Command/Event
    }

    public readonly record struct BtSnoopRecord(long Position, long TimestampMicros, ReadOnlyMemory<byte> PacketData)
    {
        private const long BtSnoopEpoch1970Us = 0x00DCDDB30F2F8000;

        public DateTime GetDateTimeUtc()
            => DateTimeOffset.UnixEpoch.AddTicks(checked(TimestampMicros - BtSnoopEpoch1970Us) * 10).UtcDateTime;
    }

    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    private bool _disposed;

    public BtSnoopReader(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        if (!stream.CanSeek)
            throw new ArgumentException("Stream must be seekable.", nameof(stream));

        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    public async IAsyncEnumerable<BtSnoopRecord> ReadAsync(
        IProgress<long>? bytesReadProgress = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        long bytesRead = 0;

        if (_stream.Length < (int)FileHeaderOffset.FileHeaderLength)
            throw new InvalidDataException("File is too small to contain a valid header.");

        byte[] fileHeader = new byte[(int)FileHeaderOffset.FileHeaderLength];
        await _stream.ReadExactlyAsync(fileHeader, ct).ConfigureAwait(false);
        bytesRead += fileHeader.Length;
        bytesReadProgress?.Report(bytesRead);

        if (!IsFileHeaderValid(fileHeader))
            throw new InvalidDataException("Invalid file header");

        byte[] recordHeader = new byte[(int)PacketRecordOffset.HeaderLength];
        while (_stream.Position < _stream.Length)
        {
            ct.ThrowIfCancellationRequested();

            if (_stream.Length - _stream.Position < recordHeader.Length)
                throw new InvalidDataException("Truncated packet record header.");

            long recordPosition = _stream.Position;
            await _stream.ReadExactlyAsync(recordHeader, ct).ConfigureAwait(false);
            bytesRead += recordHeader.Length;

            uint includedLength = GetIncludedLength(recordHeader);
            uint originalLength = GetOriginalLength(recordHeader);

            if (includedLength > int.MaxValue)
                throw new InvalidDataException("Packet record length exceeds supported size.");
            if (originalLength < includedLength)
                throw new InvalidDataException("Packet record length is invalid.");

            int recordLength = (int)includedLength;
            if (_stream.Length - _stream.Position < recordLength)
                throw new InvalidDataException("Truncated packet record payload.");

            byte[] payload = new byte[recordLength];
            await _stream.ReadExactlyAsync(payload, ct).ConfigureAwait(false);
            bytesRead += recordLength;

            bytesReadProgress?.Report(bytesRead);

            yield return new BtSnoopRecord(
                Position: recordPosition,
                TimestampMicros: GetTimestampMicroseconds(recordHeader),
                PacketData: payload);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_leaveOpen)
            await _stream.DisposeAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            if (!_leaveOpen)
                _stream.Dispose();
        }
    }

    private static uint GetOriginalLength(ReadOnlySpan<byte> packetRecordHeader)
        => BinaryPrimitives.ReadUInt32BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.OriginalLength, 4));

    private static uint GetIncludedLength(ReadOnlySpan<byte> packetRecordHeader)
        => BinaryPrimitives.ReadUInt32BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.IncludedLength, 4));

    private static uint GetPacketFlag(ReadOnlySpan<byte> packetRecordHeader)
        => BinaryPrimitives.ReadUInt32BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.PacketFlag, 4));

    private static uint GetCumulativeDrops(ReadOnlySpan<byte> packetRecordHeader)
        => BinaryPrimitives.ReadUInt32BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.CumulativeDrops, 4));

    private static long GetTimestampMicroseconds(ReadOnlySpan<byte> packetRecordHeader)
        => checked((long)BinaryPrimitives.ReadUInt64BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.TimestampMicroseconds, 8)));

    private static bool IsFileHeaderValid(ReadOnlySpan<byte> header)
    {
        string identificationPattern = Encoding.ASCII.GetString(header.Slice((int)FileHeaderOffset.IdentificationPattern, 8));
        uint versionNumber = BinaryPrimitives.ReadUInt32BigEndian(header.Slice((int)FileHeaderOffset.VersionNumber, 4));
        DatalinkCode datalinkCode = (DatalinkCode)BinaryPrimitives.ReadUInt32BigEndian(header.Slice((int)FileHeaderOffset.DatalinkType, 4));

        return identificationPattern == IdentificationPattern
               && versionNumber == VersionNumber
               && datalinkCode == DatalinkCode.H4;
    }
}
