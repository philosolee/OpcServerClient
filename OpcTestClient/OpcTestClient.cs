namespace OpcTestClient
{
    using Opc.Ua;
    using Opc.Ua.Client;
    using System.Threading;

    public class OpcTestClient : IDisposable
    {
        private readonly TextWriter output;
        private readonly ApplicationConfiguration configuration;
        private SessionReconnectHandler reconnectHandler = null!;
        private readonly object reconnectLock = new();
        private readonly string rootFolder;
        private Subscription? subscription;
        public ISession Session { get; private set; } = null!;
        public bool AutoAccept { get; set; } = false;
        public int KeepAliveInterval { get; set; } = 5000;
        public int ReconnectPeriod { get; set; } = 1000;
        public int ReconnectPeriodExponentialBackoff { get; set; } = 15000;
        public uint SessionLifeTime { get; set; } = 60 * 1000;
        public IUserIdentity UserIdentity { get; set; } = new UserIdentity();
        public string LogFile { get; set; } = null!;

        public OpcTestClient(TextWriter output, ApplicationConfiguration configuration, string root)
        {
            this.output = output;
            this.configuration = configuration;
            this.rootFolder = root;

            configuration.CertificateValidator.CertificateValidation += CertificateValidation;
        }

        public async Task<bool> ConnectAsync(string serverUrl, bool useSecurity = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                throw new ArgumentNullException(nameof(serverUrl));
            }

            try
            {
                if (Session is {Connected: true})
                {
                    await output.WriteLineAsync("Session is already connected");
                    return true;
                }

                await output.WriteLineAsync($"Connecting to... {serverUrl}");
                var endpointDescription = CoreClientUtils.SelectEndpoint(configuration, serverUrl, useSecurity);
                var endpointConfiguration = EndpointConfiguration.Create(configuration);
                var configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

                ITransportWaitingConnection? connection = null;

                var sessionFactory = TraceableSessionFactory.Instance;
                var newSession = await sessionFactory.CreateAsync(
                    configuration,
                    connection,
                    configuredEndpoint,
                    connection == null,
                    false,
                    configuration.ApplicationName,
                    SessionLifeTime,
                    UserIdentity,
                    null,
                    cancellationToken).ConfigureAwait(false);

                if (newSession != null && newSession.Connected)
                {
                    Session = newSession;
                    Session.KeepAliveInterval = KeepAliveInterval;
                    Session.DeleteSubscriptionsOnClose = false;
                    Session.TransferSubscriptionsOnReconnect = true;
                    Session.KeepAlive += Session_KeepAlive;
                    reconnectHandler = new SessionReconnectHandler(true, ReconnectPeriodExponentialBackoff);
                }
                await output.WriteLineAsync($"New session created with the SessionName = {Session?.SessionName}");

                return true;
            }
            catch (Exception ex)
            {
                await output.WriteLineAsync($"Create Session Error: {ex.Message}");
                return false;
            }
        }

        public Task<WriteResponse> WriteValueToAsync(string cell, string device, string tag, object? value,
            CancellationToken cancellationToken = default)
        {
            var nodesToWrite = new WriteValueCollection();
            var intWriteValue = new WriteValue
            {
                NodeId = new NodeId($"ns=2;s={rootFolder}.{cell}.{device}.{tag}"),
                AttributeId = Attributes.Value,
                Value = new DataValue {Value = value}
            };
            nodesToWrite.Add(intWriteValue);

            return Session.WriteAsync(null, nodesToWrite, cancellationToken);
        }

        public Task<WriteResponse> WriteValuesAsync(Dictionary<string, object> writeDictionary,
            CancellationToken cancellationToken = default)
        {
            if (writeDictionary.Count == 0)
            {
                return Task.FromResult<WriteResponse>(new WriteResponse());
            }

            var notesToWrite = new WriteValueCollection();
            var writeValues = writeDictionary.Select((entry) =>
                new WriteValue
                {
                    NodeId = new NodeId($"ns=2;s={rootFolder}.{entry.Key}"),
                    AttributeId = Attributes.Value,
                    Value = new DataValue {Value = entry.Value}
                });

            notesToWrite.AddRange(writeValues);

            return Session.WriteAsync(null, notesToWrite, cancellationToken);
        }

        public async Task<T> ReadValueFromAsync<T>(string cell, string device, string tag)
        {
            var response = await Session.ReadValueAsync(new NodeId($"ns=2;s={rootFolder}.{cell}.{device}.{tag}"));
            return (T)response.Value;
        }

        public Task RemoveNodeAsync(string cell, string device, string tag)
        {
            return RemoveNodeAsync($"ns=2;s={rootFolder}.{cell}.{device}.{tag}");
        }

        public async Task RemoveNodeAsync(string nodeIdString)
        {
            var nodeId = new NodeId(nodeIdString);

            try
            {
                var deleteCollection = new DeleteNodesItemCollection
                {
                    new() {NodeId = nodeId, DeleteTargetReferences = false}
                };

                await Session.DeleteNodesAsync(null, deleteCollection, CancellationToken.None);
            }
            catch (ServiceResultException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task MonitorTag(string cell, string device, string tag, Func<ushort, bool> action)
        {
            await CreateSubscriptionAsync();

            var monitoredItem = new MonitoredItem
            {
                AttributeId = Attributes.Value,
                StartNodeId = new NodeId($"ns=2;s={rootFolder}.{cell}.{device}.{tag}"),
                SamplingInterval = 0,
                QueueSize = 1000,
                DiscardOldest = true,
                Handle = action
            };
            monitoredItem.Notification += new MonitoredItemNotificationEventHandler(MonitoredItem_ExecuteFunction);

            subscription?.AddItem(monitoredItem);
            await subscription!.ApplyChangesAsync();
        }

        private void DisposeSubscriptions()
        {
            var items = subscription?.MonitoredItems;

            if (items is not null)
            {
                subscription?.RemoveItems(items);
                subscription!.ApplyChanges();
            }

            if (subscription != null)
            {
                Session!.RemoveSubscription(subscription);
                subscription = null;
            }
        }

        public async Task<uint> CountSubscriptions()
        {
            var response =
                await Session.ReadValueAsync(
                    new NodeId(2285));

            return (uint)response.Value;
        }

        private async Task CreateSubscriptionAsync(CancellationToken cancellationToken = default)
        {
            subscription ??= new Subscription
            {
                PublishingEnabled = true,
                PublishingInterval = 1000,
                Priority = 1,
                KeepAliveCount = 10,
                LifetimeCount = 300,
                MaxNotificationsPerPublish = 1000,
            };

            if (!Session.Subscriptions.Contains(subscription))
            {
                Session.AddSubscription(subscription);
                await subscription.CreateAsync(cancellationToken);
            }
        }

        private void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            try
            {
                // check for events from discarded sessions.
                if (!Session.Equals(session))
                {
                    return;
                }

                // start reconnect sequence on communication error.
                if (ServiceResult.IsBad(e.Status))
                {
                    if (ReconnectPeriod <= 0)
                    {
                        Utils.LogWarning("KeepAlive status {0}, but reconnect is disabled.", e.Status);
                        return;
                    }

                    var state = reconnectHandler.BeginReconnect(Session, ReconnectPeriod, Client_ReconnectComplete!);
                    if (state == SessionReconnectHandler.ReconnectState.Triggered)
                    {
                        Utils.LogInfo("KeepAlive status {0}, reconnect status {1}, reconnect period {2}ms.", e.Status,
                            state, ReconnectPeriod);
                    }
                    else
                    {
                        Utils.LogInfo("KeepAlive status {0}, reconnect status {1}.", e.Status, state);
                    }

                    // cancel sending a new keep alive request, because reconnect is triggered.
                    e.CancelKeepAlive = true;

                    return;
                }
            }
            catch (Exception exception)
            {
                Utils.LogError(exception, "Error in OnKeepAlive.");
            }
        }

        private void Client_ReconnectComplete(object sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!Object.ReferenceEquals(sender, reconnectHandler))
            {
                return;
            }

            lock (reconnectLock)
            {
                // if session recovered, Session property is null
                if (reconnectHandler.Session != null)
                {
                    // ensure only a new instance is disposed
                    // after reactivate, the same session instance may be returned
                    if (!Object.ReferenceEquals(Session, reconnectHandler.Session))
                    {
                        output.WriteLine("--- RECONNECTED TO NEW SESSION --- {0}", reconnectHandler.Session.SessionId);
                        var session = Session;
                        Session = reconnectHandler.Session;
                        Utils.SilentDispose(session);
                    }
                    else
                    {
                        output.WriteLine("--- REACTIVATED SESSION --- {0}", reconnectHandler.Session.SessionId);
                    }
                }
                else
                {
                    output.WriteLine("--- RECONNECT KeepAlive recovered ---");
                }
            }
        }

        private void MonitoredItem_ExecuteFunction(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args)
        {
            try
            {
                Func<ushort, bool>? function = monitoredItem.Handle as Func<ushort, bool>;

                if (function == null)
                {
                    return;
                }

                if (args.NotificationValue is not MonitoredItemNotification value)
                {
                    return;
                }

                var newValue = (ushort)value.Value.WrappedValue.Value;

                function.Invoke(newValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing subscription function");
                Console.WriteLine(ex.ToString());
            }
        }

        protected virtual void CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            bool certificateAccepted = false;

            // ****
            // Implement a custom logic to decide if the certificate should be
            // accepted or not and set certificateAccepted flag accordingly.
            // The certificate can be retrieved from the e.Certificate field
            // ***

            ServiceResult error = e.Error;
            output.WriteLine(error);
            if (error.StatusCode == StatusCodes.BadCertificateUntrusted && AutoAccept)
            {
                certificateAccepted = true;
            }

            if (certificateAccepted)
            {
                output.WriteLine("Untrusted Certificate accepted. Subject = {0}", e.Certificate.Subject);
                e.Accept = true;
            }
            else
            {
                output.WriteLine("Untrusted Certificate rejected. Subject = {0}", e.Certificate.Subject);
            }
        }

        public void Dispose()
        {
            DisposeSubscriptions();
            output.Dispose();
            reconnectHandler.Dispose();
            Session.Dispose();
        }
    }
}