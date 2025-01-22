namespace OpcUaTestServer.Exceptions
{
    using OpcUaTestServer.Utilities;
    using System;

    /// <summary>
    /// An exception that occured and caused an exit of the application.
    /// </summary>
    [Serializable]
    public class ErrorExitException : Exception
    {
        public ExitCode ExitCode { get; }

        public ErrorExitException(ExitCode exitCode)
        {
            ExitCode = exitCode;
        }

        public ErrorExitException()
        {
            ExitCode = ExitCode.Ok;
        }

        public ErrorExitException(string message) : base(message)
        {
            ExitCode = ExitCode.Ok;
        }

        public ErrorExitException(string message, ExitCode exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        public ErrorExitException(string message, Exception innerException) : base(message, innerException)
        {
            ExitCode = ExitCode.Ok;
        }

        public ErrorExitException(string message, Exception innerException, ExitCode exitCode) : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }
}
