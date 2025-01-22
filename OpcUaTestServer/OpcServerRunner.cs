namespace OpcUaTestServer
{
    using Opc.Ua.Server;
    using Server;
    using System.Diagnostics;

    public class OpcServerRunner : IDisposable, IAsyncDisposable
    {
        public string? ServerAddress { get; set; } = string.Empty;

        private ServerHost<TestServer>? server;
        private Task? serverTask;
        private const string ApplicationName = "Test Server";
        private const string ConfigName = "Taro.OpcUaTestServer";

        public OpcServerRunner()
        {
            TextWriter output = Console.Out;

            server = new ServerHost<TestServer>(output)
            {
                AutoAccept = true,
                Password = string.Empty
            };
        }

        public async Task CreateAndStartServer(IList<INodeManagerFactory> factories)
        {
            if (server == null)
            {
                throw new NullReferenceException("server has been disposed already");
            }

            await server.LoadAsync(ApplicationName, ConfigName);
            await server.CheckCertificateAsync(false);
            server.Create(factories);

            StartServer();
        }

        public void StartServer()
        {
            if (server == null)
            {
                throw new NullReferenceException("server has been disposed already");
            }

            serverTask = Task.Run(server.StartAsync);

            WaitForServerStart(TimeSpan.FromSeconds(20));
        }

        public void StopServer()
        {
            server?.StopServer();
            serverTask?.Dispose();
            serverTask = null;
        }

        private void WaitForServerStart(TimeSpan timeout)
        {
            IEnumerable<string>? endpoints = null;
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            while ((endpoints == null || !endpoints.Any() || serverTask?.Exception != null))
            {
                endpoints = server?.Endpoints;
                if (stopwatch.Elapsed > timeout)
                {
                    break;
                }

                Thread.Sleep(300);
            }
            stopwatch.Stop();

            if (endpoints is null)
            {
                throw new ApplicationException($"OPC Server failed to start after {timeout.Seconds} seconds", serverTask?.Exception);
            }

            ServerAddress = endpoints?.FirstOrDefault();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (server != null)
            {
                await server.DisposeAsync().ConfigureAwait(false);
            }

            if (serverTask is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                serverTask?.Dispose();
            }
            server = null;
            serverTask = null;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (server is IDisposable serverDisposable)
                {
                    serverDisposable.Dispose();
                }
                else if (server != null)
                {
                    _ = server.DisposeAsync().AsTask();
                }
                
                serverTask?.Dispose();
                server = null;
                serverTask = null;
            }
        }
    }
}
