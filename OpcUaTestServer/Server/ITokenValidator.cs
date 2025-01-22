namespace OpcUaTestServer.Server
{
    using Opc.Ua;

    public interface ITokenValidator
    {
        IUserIdentity? ValidateToken(IssuedIdentityToken issuedToken);
    }
}
