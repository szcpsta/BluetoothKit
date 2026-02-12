// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BluetoothKit.LogTypes.Power;

public class Pt5Parser : IPowerSampleParser
{
    private const long HeaderOffset = 0;
    private const long StatusOffset = 272;
    private const long SampleOffset = 1024;

    private const short MissingRawCurrent = unchecked((short)0x8001);

    [Flags] // bitwise-maskable
    private enum CaptureMask : ushort
    {
        ChanMain = 0x1000,
        ChanUsb = 0x2000,
        ChanAux = 0x4000,
        ChanMarker = 0x8000,
        ChanMask = 0xf000,
    }

    private struct Sample
    {
        public long SampleIndex;   // 0...N-1

        public bool MainPresent;   // whether Main was recorded
        public double MainCurrent; // current in milliamps

        public bool UsbPresent;    // whether Usb was recorded
        public double UsbCurrent;  // current in milliamps

        public bool AuxPresent;    // whether Aux was recorded
        public double AuxCurrent;  // current in milliamps

        public bool Missing;       // true if this sample was missing
    }

    private readonly BinaryReader _pt5Reader;

    private Sample _sample;
    private readonly long _bytesPerSample;

    // Header
    private readonly DateTime _captureDate;
    private readonly CaptureMask _captureDataMask;
    private readonly ulong _sampleCount;
    private readonly float _avgMainCurrent;

    // StatusPacket
    private readonly byte _sampleRate; // (kHz)
    private readonly double _secondsPerSample;

    private bool _disposed;

    public Pt5Parser(string pt5FilePath)
        : this(File.Open(pt5FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), leaveOpen: false)
    {
    }

    public Pt5Parser(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        if (!stream.CanSeek)
            throw new ArgumentException("Stream must be seekable.", nameof(stream));

        BinaryReader reader;
        try
        {
            reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen);
        }
        catch
        {
            if (!leaveOpen)
                stream.Dispose();
            throw;
        }

        try
        {
            long oldPos = reader.BaseStream.Position;

            reader.BaseStream.Position = HeaderOffset + 28;
            _captureDate = DateTime.FromBinary(reader.ReadInt64());

            reader.BaseStream.Position = HeaderOffset + 158;
            _captureDataMask = (CaptureMask)reader.ReadUInt16();
            _sampleCount = reader.ReadUInt64();

            ulong missingCount = reader.ReadUInt64();

            reader.BaseStream.Position = HeaderOffset + 180;
            _avgMainCurrent = reader.ReadSingle() / Math.Max(1, _sampleCount - missingCount);

            reader.BaseStream.Position = oldPos;

            reader.BaseStream.Position = StatusOffset + 28;
            _sampleRate = reader.ReadByte();

            reader.BaseStream.Position = oldPos;

            _secondsPerSample = 1 / (1000.0 * _sampleRate);

            bool hasMainChannel = (_captureDataMask & CaptureMask.ChanMain) != 0;
            bool hasUsbChannel = (_captureDataMask & CaptureMask.ChanUsb) != 0;
            bool hasAuxChannel = (_captureDataMask & CaptureMask.ChanAux) != 0;

            long bytesPerSample = sizeof(ushort); // voltage is always present
            // Add lengths for optional current channels
            if (hasMainChannel)
                bytesPerSample += sizeof(uint);

            if (hasUsbChannel)
                bytesPerSample += sizeof(uint);

            if (hasAuxChannel)
                bytesPerSample += sizeof(uint);

            _bytesPerSample = bytesPerSample;

            _sample = new Sample
            {
                SampleIndex = -1,
                MainPresent = hasMainChannel,
                UsbPresent = hasUsbChannel,
                AuxPresent = hasAuxChannel,
            };

            _pt5Reader = reader;
        }
        catch
        {
            reader.Dispose();
            throw;
        }
    }

    public long SampleCount => (long)_sampleCount;
    public double Period => _secondsPerSample;
    public double AverageCurrent => _avgMainCurrent;
    public DateTime CaptureDate => _captureDate;

    public double GetTimestamp(long index) => index * _secondsPerSample;

    public bool TryGetCurrent(long index, out double current)
    {
        if (_sample.SampleIndex != index)
        {
            GetSample(index);
        }

        if (_sample.Missing)
        {
            current = 0;
            return false;
        }

        current = _sample.MainCurrent;
        return true;
    }

    public double GetCurrent(long index)
    {
        if (TryGetCurrent(index, out var current))
            return current;

        return MissingRawCurrent;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // 여기서 관리 리소스를 정리한다.
            _pt5Reader.Dispose();
        }

        // 여기서 비관리 리소스를 정리한다.
        _disposed = true;
    }

    private void GetSample(long sampleIndex)
    {
        var reader = _pt5Reader;

        // Remember the index and time
        _sample.SampleIndex = sampleIndex;
        _sample.Missing = false;

        // Remember original position
        long oldPos = reader.BaseStream.Position;

        // Position the file to the start of the desired sample (if necessary)
        long newPos = SampleOffset + _bytesPerSample * sampleIndex;
        if (oldPos != newPos)
            reader.BaseStream.Position = newPos;

        // Main current (mA)
        if (_sample.MainPresent)
        {
            int raw = reader.ReadInt32();

            _sample.Missing = _sample.Missing ||
                              raw == MissingRawCurrent;
            if (!_sample.Missing)
            {
                _sample.MainCurrent = raw / 1000f;   // uA -> mA
            }
        }

        // USB current (mA)
        if (_sample.UsbPresent)
        {
            int raw = reader.ReadInt32();

            _sample.Missing = _sample.Missing ||
                              raw == MissingRawCurrent;
            if (!_sample.Missing)
            {
                _sample.UsbCurrent = raw / 1000f;   // uA -> mA
            }
        }

        // Aux current (mA)
        if (_sample.AuxPresent)
        {
            int raw = reader.ReadInt32();

            _sample.Missing = _sample.Missing ||
                              raw == MissingRawCurrent;
            if (!_sample.Missing)
            {
                _sample.AuxCurrent = raw / 1000f;   // uA -> mA
            }
        }
    }
}
