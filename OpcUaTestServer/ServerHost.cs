namespace OpcUaTestServer
{
    using Exceptions;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using Opc.Ua.Server;
    using Utilities;
    using System.Text;


    public class ServerHost<T>(TextWriter output) : IAsyncDisposable
        where T : StandardServer, new()
    {
        private ApplicationInstance? application;
        private DateTime? lastEventTime;
        private Task? status;
        public bool AutoAccept { get; set; }
        public T? Server { get; private set; }
        public string? Password { get; set; }
        public IEnumerable<string>? Endpoints { get; private set; }
        public ExitCode ExitCode { get; private set; }

        public async Task LoadAsync(string applicationName, string configNamePrefix)
        {
            try
            {
                ExitCode = ExitCode.ErrorNotStarted;

                var passwordProvider = new CertificatePasswordProvider(Password);

                application = new ApplicationInstance
                {
                    ApplicationName = applicationName,
                    ApplicationType = ApplicationType.Server,
                    ConfigSectionName = configNamePrefix,
                    CertificatePasswordProvider = passwordProvider
                };

                await application.LoadApplicationConfiguration(false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                output.WriteLine(ex);
                throw new ErrorExitException(ex.Message, ExitCode);
            }
        }

        public async Task CheckCertificateAsync(bool renewCertificate)
        {
            try
            {
                if (application == null)
                {
                    throw new NullReferenceException("Call the LoadAsync method first to load the application");
                }

                var config = application.ApplicationConfiguration;
                if (renewCertificate)
                {
                    await application.DeleteApplicationInstanceCertificate().ConfigureAwait(false);
                }

                // check the application certificate.
                bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, minimumKeySize: 0).ConfigureAwait(false);
                if (!haveAppCertificate)
                {
                    throw new Exception("Application instance certificate invalid!");
                }

                if (!config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
                }
            }
            catch (Exception ex)
            {
                output.WriteLine(ex);
                throw new ErrorExitException(ex.Message, ExitCode);
            }
        }

        public void Create(IList<INodeManagerFactory>? nodeManagerFactories)
        {
            try
            {
                Server = new T();

                if (nodeManagerFactories != null)
                {
                    foreach (var factory in nodeManagerFactories)
                    {
                        Server.AddNodeManager(factory);
                    }
                }
            }
            catch (Exception ex)
            {
                output.WriteLine(ex);
                throw new ErrorExitException(ex.Message, ExitCode);
            }
        }

        public async Task StartAsync()
        {
            try
            {
                if (application == null)
                {
                    throw new NullReferenceException("Call the LoadAsync method first to load the application");
                }
                Server ??= new T();

                await application.Start(Server).ConfigureAwait(false);

                Endpoints = application.Server.GetEndpoints().Select(e => e.EndpointUrl).Distinct();

                foreach (string endpoint in Endpoints)
                {
                    await output.WriteLineAsync(endpoint);
                }

                Server.CurrentInstance.DiagnosticsNodeManager.SetDiagnosticsEnabled(Server.CurrentInstance.DefaultSystemContext, true);

                status = Task.Run(StatusThreadAsync);

                SetupEventNotifications();
            }
            catch (Exception ex)
            {
                output.WriteLine(ex);
                throw new ErrorExitException(ex.Message, ExitCode);
            }
        }

        public void StopServer()
        {
            Server?.Stop();
            Endpoints = [];
        }

        private void SetupEventNotifications()
        {
            if (Server == null)
            {
                throw new NullReferenceException("Need to call StartAsync or LoadAsync First.");
            }

            Server.CurrentInstance.SessionManager.SessionActivated += EventStatus;
            Server.CurrentInstance.SessionManager.SessionCreated += EventStatus;
            Server.CurrentInstance.SessionManager.SessionClosing += EventStatus;
            Server.CurrentInstance.SubscriptionManager.SubscriptionCreated += SubscriptionManager_SubscriptionChanged;
            Server.CurrentInstance.SubscriptionManager.SubscriptionDeleted += SubscriptionManager_SubscriptionChanged;
        }

        private void SubscriptionManager_SubscriptionChanged(Subscription subscription, bool deleted)
        {
            lastEventTime = DateTime.UtcNow;
            PrintSubscriptionStatus(subscription, deleted);
        }

        private void PrintSubscriptionStatus(Subscription subscription, bool deleted)
        {
            StringBuilder item = new();

            lock (subscription.DiagnosticsLock)
            {
                item.Append($"{DateTime.UtcNow}: ");
                item.Append("Subscription Change Event Fired by ");
                if (subscription.Session.Identity != null)
                {
                    item.Append($":{subscription.Session.Identity.DisplayName}");
                }

                item.Append($"SubId: {subscription.Id} ; ");
                item.Append($"Number of items to monitor is: {subscription.MonitoredItemCount} ; ");
                item.Append($"Deleted Flag: {deleted} ; ");
                item.Append($"SessionId: {subscription.Session.Id}");
            }

            output.WriteLine(item.ToString());
        }

        private void EventStatus(Session session, SessionEventReason reason)
        {
            lastEventTime = DateTime.UtcNow;
            PrintSessionStatus(session, reason.ToString());
        }

        private void PrintSessionStatus(Session session, string reason, bool lastContact = false)
        {
            StringBuilder item = new();
            lock (session.DiagnosticsLock)
            {
                item.Append($"{DateTime.UtcNow}: ");
                item.Append($"{reason,9}:{session.SessionDiagnostics.SessionName,20}:");
                if (lastContact)
                {
                    item.Append($"Last Event: {session.SessionDiagnostics.ClientLastContactTime.ToLocalTime():HH:mm:ss}");
                }
                else
                {
                    if (session.Identity != null)
                    {
                        item.Append($":{session.Identity.DisplayName,20}");
                    }

                    item.Append($":{session.Id}");
                }
            }
            output.WriteLine(item.ToString());
        }

        private async Task StatusThreadAsync()
        {
            while (Server != null)
            {
                if (DateTime.UtcNow - lastEventTime > TimeSpan.FromMilliseconds(10000))
                {
                    IList<Session> sessions = Server.CurrentInstance.SessionManager.GetSessions();
                    for (int ii = 0; ii < sessions.Count; ii++)
                    {
                        Session session = sessions[ii];
                        PrintSessionStatus(session, "-Status-", true);
                    }
                    lastEventTime = DateTime.UtcNow;
                }
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode != StatusCodes.BadCertificateUntrusted) return;

            if (AutoAccept)
            {
                e.Accept = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (Server != null)
                {
                    using T server = Server;
                    // Stop status thread
                    Server = null;
                    await status!.ConfigureAwait(false);

                    // Stop server and dispose
                    server.Stop();
                }

                ExitCode = ExitCode.Ok;
            }
            catch (Exception ex)
            {
                throw new ErrorExitException(ex.Message, ExitCode.ErrorStopping);
            }
        }
    }
}
