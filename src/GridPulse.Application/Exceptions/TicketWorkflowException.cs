namespace GridPulse.Application.Exceptions;

public sealed class TicketWorkflowException : InvalidOperationException
{
    public TicketWorkflowException(string message)
        : base(message)
    {
    }
}