namespace SupportSystem.Domain;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Direction Direction { get; set; }
    public string Body { get; set; } = default!;
    public Guid? SentByAgentId { get; set; }
    public bool IsAiGenerated { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
