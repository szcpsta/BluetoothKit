// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BluetoothKit.LogTypes.Power;

namespace BluetoothKit.Tests.LogTypes.Power;

public class Pt5ParserTests
{
    [Fact]
    public void ArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => new Pt5Parser(pt5FilePath: null!));
    }

    [Fact]
    public void ArgumentNullExceptionTest_ForStream()
    {
        Assert.Throws<ArgumentNullException>(() => new Pt5Parser(stream: null!));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        using var reader = new Pt5Parser(OpenSamplePt5());

        reader.Dispose();
        reader.Dispose();
    }

    [Fact]
    public void Dispose_ShouldCloseStream_WhenLeaveOpenIsFalse()
    {
        var stream = OpenSamplePt5();
        var reader = new Pt5Parser(stream, leaveOpen: false);

        reader.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }

    [Fact]
    public void Dispose_ShouldNotCloseStream_WhenLeaveOpenIsTrue()
    {
        var stream = OpenSamplePt5();
        var reader = new Pt5Parser(stream, leaveOpen: true);

        reader.Dispose();

        Assert.Equal(0x90, stream.ReadByte());
        stream.Dispose();
    }

    [Fact]
    public void ParseHeader_FromSamplePt5()
    {
        using var parser = new Pt5Parser(OpenSamplePt5());

        Assert.Equal(801166, parser.SampleCount);
        Assert.Equal(0.0002, parser.Period);
        Assert.Equal(610.9344, parser.AverageCurrent, precision: 3);
        Assert.Equal(0x8D2BAC8CD578344, parser.CaptureDate.ToUniversalTime().Ticks);
    }

    [Fact]
    public void ParseSamples_FromSamplePt5()
    {
        using var parser = new Pt5Parser(OpenSamplePt5());

        Assert.Equal(4.138, parser.GetCurrent(0), precision: 3);
        Assert.Equal(4.138, parser.GetCurrent(78), precision: 3);
        Assert.Equal(4.164, parser.GetCurrent(1234), precision: 3);
        Assert.Equal(4.134, parser.GetCurrent(4321), precision: 3);
        Assert.Equal(-0.028, parser.GetCurrent(parser.SampleCount - 1), precision: 3);
    }

    private static FileStream OpenSamplePt5()
    {
        var path = GetSamplePt5Path();
        return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    private static string GetSamplePt5Path()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Sample.pt5");
        if (!File.Exists(path))
            throw new FileNotFoundException("Sample pt5 not found.");

        return path;
    }
}
