// See https://aka.ms/new-console-template for more information

using BluetoothKit.Console.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddBranch("power", power =>
    {
        power.AddCommand<PowerSummaryCommand>("summary");
        power.AddCommand<PowerExtractCommand>("extract");
    });

    config.AddBranch("hci", hci =>
    {
        hci.AddCommand<HciSummaryCommand>("summary");
        hci.AddCommand<HciExtractCommand>("extract");
        hci.AddCommand<HciFilterCommand>("filter");
    });
});

return app.Run(args);
