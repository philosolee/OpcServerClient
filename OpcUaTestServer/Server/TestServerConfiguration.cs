namespace OpcUaTestServer.Server
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = ServerNamespaces.TestServer)]
    public class TestServerConfiguration
    {
        public TestServerConfiguration()
        {
            Initialise();
        }

        private void Initialise(StreamingContext context)
        {
            Initialise();
        }

        private void Initialise()
        {
        }
    }
}
