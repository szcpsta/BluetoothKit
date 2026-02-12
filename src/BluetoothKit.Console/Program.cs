// See https://aka.ms/new-console-template for more information

using BluetoothKit.Console.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddBranch("power", add =>
    {
        add.AddCommand<SummaryCommand>("summary");
        add.AddCommand<ExtractCommand>("extract");
    });
});

return app.Run(args);
