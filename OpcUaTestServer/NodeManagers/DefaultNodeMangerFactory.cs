namespace OpcUaTestServer.NodeManagers
{
    using Opc.Ua;
    using Opc.Ua.Server;

    public class DefaultNodeMangerFactory(string rootFolder) : INodeManagerFactory
    {
        public INodeManager Create(IServerInternal server, ApplicationConfiguration configuration)
        {
            return new DefaultNodeManager(server, configuration, rootFolder);
        }

        public StringCollection NamespacesUris { get; } = null!;
    }
}
