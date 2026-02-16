// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using BluetoothKit.LogTypes.Hci.Common;

namespace BluetoothKit.Console.Commands;

internal sealed record FilterSpec(
    HashSet<byte> Ogfs,
    HashSet<ushort> Ocfs,
    HashSet<ushort> Opcodes,
    HashSet<byte> EventCodes,
    HashSet<byte> LeSubevents)
{
    public static FilterSpec CreateDefault()
        => new(new HashSet<byte>(), new HashSet<ushort>(), new HashSet<ushort>(), new HashSet<byte>(), new HashSet<byte>());

    public bool IsEmpty => !HasCommandFilters && !HasEventFilters;

    public bool HasCommandFilters => Ogfs.Count != 0 || Ocfs.Count != 0 || Opcodes.Count != 0;
    public bool HasEventFilters => EventCodes.Count != 0 || LeSubevents.Count != 0;

    public FilterSpec Merge(FilterSpec other)
        => new(
            new HashSet<byte>(Ogfs.Concat(other.Ogfs)),
            new HashSet<ushort>(Ocfs.Concat(other.Ocfs)),
            new HashSet<ushort>(Opcodes.Concat(other.Opcodes)),
            new HashSet<byte>(EventCodes.Concat(other.EventCodes)),
            new HashSet<byte>(LeSubevents.Concat(other.LeSubevents)));

    public bool MatchesCommand(HciOpcode opcode)
    {
        if (IsEmpty || !HasCommandFilters)
            return false;
        if (Ogfs.Count != 0 && !Ogfs.Contains(opcode.Ogf))
            return false;
        if (Ocfs.Count != 0 && !Ocfs.Contains(opcode.Ocf))
            return false;
        if (Opcodes.Count != 0 && !Opcodes.Contains(opcode.Value))
            return false;

        return true;
    }

    public bool MatchesEvent(HciEventPacket packet)
    {
        if (IsEmpty || !HasEventFilters)
            return false;
        if (EventCodes.Count != 0 && !EventCodes.Contains(packet.EventCode.Value))
            return false;
        if (LeSubevents.Count != 0)
        {
            if (packet.EventCode.Value != 0x3E)
                return false;

            var span = packet.Parameters.Span;
            if (span.Length == 0)
                return false;

            if (!LeSubevents.Contains(span[0]))
                return false;
        }

        return true;
    }
}
