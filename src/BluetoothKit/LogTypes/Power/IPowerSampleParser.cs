// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BluetoothKit.LogTypes.Power;

public interface IPowerSampleParser : IDisposable
{
    long SampleCount { get; }

    double Period { get; }

    double AverageCurrent { get; }

    DateTime CaptureDate { get; }

    double GetTimestamp(long index);

    double GetCurrent(long index);
}
