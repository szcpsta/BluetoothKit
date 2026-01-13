// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Text;

namespace BtSnoop.Reader;

public sealed class BtSnoopReader : IDisposable, IAsyncDisposable
{
    public readonly record struct BtSnoopRecord(long Position, long TimestampMicros, ReadOnlyMemory<byte> PacketData)
    {
        private const long BtSnoopEpoch1970Us = 0x00DCDDB30F2F8000;

        public DateTime GetDateTimeUtc()
            => DateTimeOffset.UnixEpoch.AddTicks(checked(TimestampMicros - BtSnoopEpoch1970Us) * 10).UtcDateTime;
    }

    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    private const string IdentificationPattern = "btsnoop\0";
    private const uint VersionNumber = 1;

    private bool _disposed;

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
        H5 = 1004
    }

    [Flags]
    private enum PacketFlagBit
    {
        Direction = 0,  // Direction flag 0 = Sent, 1 = Received
        Command = 1,    // Command flag 0 = Data, 1 = Command/Event
    }

    public BtSnoopReader(Stream? stream, bool leaveOpen = false)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _leaveOpen = leaveOpen;
    }

    public async IAsyncEnumerable<BtSnoopRecord> ReadAsync(
        IProgress<long>? bytesReadProgress = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        long bytesRead = 0;

        byte[] fileHeader = new byte[(int)FileHeaderOffset.FileHeaderLength];
        await _stream.ReadExactlyAsync(fileHeader, ct).ConfigureAwait(false);
        bytesRead += fileHeader.Length;

        if (!IsFileHeaderValid(fileHeader))
        {
            throw new InvalidDataException("Invalid file header");
        }

        byte[] recordHeader = new byte[(int)PacketRecordOffset.HeaderLength];
        while (_stream.Position < _stream.Length)
        {
            ct.ThrowIfCancellationRequested();

            long recordPosition = _stream.Position;
            await _stream.ReadExactlyAsync(recordHeader, ct).ConfigureAwait(false);
            bytesRead += recordHeader.Length;

            int recordLength = (int)GetIncludedLength(recordHeader);
            byte[] payload = new byte[recordLength];
            await _stream.ReadExactlyAsync(payload, ct).ConfigureAwait(false);
            bytesRead += recordLength;

            bytesReadProgress?.Report(bytesRead);

            yield return new BtSnoopRecord(Position: recordPosition,
                                            TimestampMicros: GetTimestampMicroseconds(recordHeader),
                                            PacketData: payload);
        }
    }

    private uint GetOriginalLength(ReadOnlySpan<byte> packetRecordHeader)
        => BinaryPrimitives.ReadUInt32BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.OriginalLength, 4));

    private uint GetIncludedLength(ReadOnlySpan<byte> packetRecordHeader)
        => BinaryPrimitives.ReadUInt32BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.IncludedLength, 4));

    private uint GetPacketFlag(ReadOnlySpan<byte> packetRecordHeader)
        => BinaryPrimitives.ReadUInt32BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.PacketFlag, 4));

    private uint GetCumulativeDrops(ReadOnlySpan<byte> packetRecordHeader)
        => BinaryPrimitives.ReadUInt32BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.CumulativeDrops, 4));

    private long GetTimestampMicroseconds(ReadOnlySpan<byte> packetRecordHeader)
        => (long)BinaryPrimitives.ReadUInt64BigEndian(packetRecordHeader.Slice((int)PacketRecordOffset.TimestampMicroseconds, 8));

    private bool IsFileHeaderValid(ReadOnlySpan<byte> header)
    {
        string identificationPattern = Encoding.ASCII.GetString(header.ToArray(), (int)FileHeaderOffset.IdentificationPattern, 8);
        uint versionNumber = BinaryPrimitives.ReadUInt32BigEndian(header.Slice((int)FileHeaderOffset.VersionNumber, 4));
        DatalinkCode datalinkCode = (DatalinkCode)BinaryPrimitives.ReadUInt32BigEndian(header.Slice((int)FileHeaderOffset.DatalinkType, 4));

        return identificationPattern == IdentificationPattern
               && versionNumber == VersionNumber
               && datalinkCode == DatalinkCode.H4;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_leaveOpen)
            await _stream.DisposeAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }
}
