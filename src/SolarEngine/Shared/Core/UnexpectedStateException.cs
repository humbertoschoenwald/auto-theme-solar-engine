namespace SolarEngine.Shared.Core;

internal sealed class UnexpectedStateException : Exception
{
    public UnexpectedStateException(string message)
        : base(message)
    {
    }

    public UnexpectedStateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
