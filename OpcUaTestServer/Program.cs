// See https://aka.ms/new-console-template for more information

using Opc.Ua;
using OpcUaTestServer;
using OpcUaTestServer.Server;
using System.Globalization;
using OpcUaTestServer.Utilities;

TextWriter output = Console.Out;
output.WriteLine("Welcome to the fantastic OPC UA Test Server... :-D");

output.WriteLine("OPC UA library: {0} @ {1} -- {2}",
    Utils.GetAssemblyBuildNumber(),
    Utils.GetAssemblyTimestamp().ToString("G", CultureInfo.InvariantCulture),
    Utils.GetAssemblySoftwareVersion());

var applicationName = "Test Server";
var configName = "Taro.OpcUaTestServer";

try
{
    var server = new ServerHost<TestServer>(output)
    {
        AutoAccept = true, 
        Password = string.Empty
    };

    await server.LoadAsync(applicationName, configName);

    await server.CheckCertificateAsync(false);

    server.Create(null);

    await server.StartAsync();

    var quitCTS = new CancellationTokenSource();
    var quitEvent = ConsoleUtils.CtrlCHandler(quitCTS);

    quitEvent.WaitOne(Timeout.Infinite);
}
catch (Exception ex)
{
    output.WriteLine(ex);
}




