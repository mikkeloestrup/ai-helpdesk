namespace SupportSystem.Domain;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TicketNumber { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string CustomerEmail { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Description { get; set; } = default!;

    public TicketStatus Status { get; private set; } = TicketStatus.Ny;
    public Priority Priority { get; set; } = Priority.Normal;

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid? AssignedToAgentId { get; set; }
    public Agent? AssignedToAgent { get; set; }
    public string? AssignedToTeam { get; set; }

    public Guid? LockedByAgentId { get; set; }
    public DateTime? LockedUntil { get; set; }

    public DateTime? SlaDeadline { get; set; }
    public bool SlaBreach { get; set; }
    public int? ResolutionMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public ClosureReason? ClosureReason { get; set; }

    public AiAnalysis? AiAnalysis { get; set; }
    public List<Message> Messages { get; } = [];
    public List<Note> Notes { get; } = [];

    /// <summary>Gyldige statusovergange jf. forretningsregler.md.</summary>
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.Ny] = [TicketStatus.UnderAiAnalyse],
        [TicketStatus.UnderAiAnalyse] = [TicketStatus.ÅbenUtildelt, TicketStatus.Eskaleret],
        [TicketStatus.ÅbenUtildelt] = [TicketStatus.ÅbenTildelt, TicketStatus.Lukket],
        [TicketStatus.ÅbenTildelt] = [TicketStatus.AfventerKunde, TicketStatus.Lukket],
        [TicketStatus.AfventerKunde] = [TicketStatus.ÅbenTildelt, TicketStatus.Lukket],
        [TicketStatus.Eskaleret] = [TicketStatus.ÅbenTildelt],
        [TicketStatus.Lukket] = [],
    };

    public bool CanTransitionTo(TicketStatus newStatus) =>
        AllowedTransitions[Status].Contains(newStatus);

    /// <summary>Skifter status hvis overgangen er gyldig, ellers kastes <see cref="InvalidStatusTransitionException"/>.</summary>
    public void Transition(TicketStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidStatusTransitionException(Status, newStatus);

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
