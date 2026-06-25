namespace SupportSystem.Domain;

/// <summary>Intern note — vises kun for agenter.</summary>
public class Note
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public string Body { get; set; } = default!;
    public Guid CreatedByAgentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
