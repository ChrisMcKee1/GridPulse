namespace GridPulse.Application.Exceptions;

public sealed class DispatchWorkflowException : InvalidOperationException
{
    public DispatchWorkflowException(string message)
        : base(message)
    {
    }
}
