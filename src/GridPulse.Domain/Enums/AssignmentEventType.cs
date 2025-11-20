namespace GridPulse.Domain.Enums;

public enum AssignmentEventType
{
    TicketCreated = 0,
    TicketEdited = 1,
    RecommendationGenerated = 2,
    AssignmentPublished = 3,
    CrewAcknowledged = 4,
    CrewEnRoute = 5,
    CrewOnScene = 6,
    CrewPaused = 7,
    CrewCompleted = 8,
    OperatorClosed = 9,
    TicketCancelled = 10,
    TicketDuplicateFlag = 11
}
