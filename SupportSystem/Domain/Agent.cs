namespace SupportSystem.Domain;

public class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Team { get; set; }
    public bool IsActive { get; set; } = true;
}
