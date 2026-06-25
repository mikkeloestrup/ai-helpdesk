namespace SupportSystem.Domain;

/// <summary>Kastes ved en ugyldig statusovergang. Mappes til HTTP 422 i API-laget.</summary>
public sealed class InvalidStatusTransitionException(TicketStatus from, TicketStatus to)
    : Exception($"Ugyldig statusovergang: {from} → {to}")
{
    public TicketStatus From { get; } = from;
    public TicketStatus To { get; } = to;
}
