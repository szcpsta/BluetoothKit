// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BluetoothKit.Console.Commands;

internal static class HciFilterSets
{
    internal sealed record FilterSet(
        int Id,
        string Name,
        string Description,
        FilterSpec Spec);

    private static readonly IReadOnlyDictionary<int, FilterSet> Sets = new Dictionary<int, FilterSet>
    {
        [1] = new FilterSet(
            1,
            "le-adv-scan",
            "LE legacy + extended advertising/scan commands and related LE Meta subevents.",
            new FilterSpec(
                new HashSet<byte> { 0x08 },
                new HashSet<ushort>
                {
                    0x0006, // HCI_LE_Set_Advertising_Parameters
                    0x0007, // HCI_LE_Read_Advertising_Physical_Channel_Tx_Power
                    0x0008, // HCI_LE_Set_Advertising_Data
                    0x0009, // HCI_LE_Set_Scan_Response_Data
                    0x000A, // HCI_LE_Set_Advertising_Enable
                    0x000B, // HCI_LE_Set_Scan_Parameters
                    0x000C, // HCI_LE_Set_Scan_Enable
                    0x0035, // HCI_LE_Set_Advertising_Set_Random_Address
                    0x0036, // HCI_LE_Set_Extended_Advertising_Parameters [v1]
                    0x0037, // HCI_LE_Set_Extended_Advertising_Data
                    0x0038, // HCI_LE_Set_Extended_Scan_Response_Data
                    0x0039, // HCI_LE_Set_Extended_Advertising_Enable
                    0x003A, // HCI_LE_Read_Maximum_Advertising_Data_Length
                    0x003B, // HCI_LE_Read_Number_of_Supported_Advertising_Sets
                    0x003C, // HCI_LE_Remove_Advertising_Set
                    0x003D, // HCI_LE_Clear_Advertising_Sets
                    0x0041, // HCI_LE_Set_Extended_Scan_Parameters
                    0x0042, // HCI_LE_Set_Extended_Scan_Enable
                    0x007F, // HCI_LE_Set_Extended_Advertising_Parameters [v2]
                },
                new HashSet<ushort>(),
                new HashSet<byte>
                {
                    0x3E, // LE Meta event (subevent code filter further constrains)
                },
                new HashSet<byte>
                {
                    0x02, // HCI_LE_Advertising_Report
                    0x0B, // HCI_LE_Directed_Advertising_Report
                    0x0D, // HCI_LE_Extended_Advertising_Report
                    0x11, // HCI_LE_Scan_Timeout
                    0x12, // HCI_LE_Advertising_Set_Terminated
                    0x13, // HCI_LE_Scan_Request_Received
                },
                new HashSet<ushort>()))
    };

    public static bool TryGet(int id, out FilterSet set)
        => Sets.TryGetValue(id, out set!);

    public static string DescribeKnownSets()
        => string.Join(", ", Sets.Values.OrderBy(s => s.Id).Select(s => $"{s.Id}:{s.Name}"));
}
