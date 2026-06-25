using FluentAssertions;
using SupportSystem.Domain;

namespace SupportSystem.Tests;

public class TicketStatusMachineTests
{
    private static Ticket NewTicket() => new()
    {
        TicketNumber = "TKT-2026-00001",
        CustomerName = "Test Kunde",
        CustomerEmail = "test@example.dk",
        Subject = "Emne",
        Description = "Beskrivelse",
    };

    [Theory]
    [InlineData(TicketStatus.UnderAiAnalyse)]
    public void Transition_TilGyldigStatus_OpdatererStatus(TicketStatus target)
    {
        var ticket = NewTicket();

        ticket.Transition(target);

        ticket.Status.Should().Be(target);
    }

    [Fact]
    public void Transition_GennemFuldtFlow_ErTilladt()
    {
        var ticket = NewTicket();

        ticket.Transition(TicketStatus.UnderAiAnalyse);
        ticket.Transition(TicketStatus.ÅbenUtildelt);
        ticket.Transition(TicketStatus.ÅbenTildelt);
        ticket.Transition(TicketStatus.AfventerKunde);
        ticket.Transition(TicketStatus.Lukket);

        ticket.Status.Should().Be(TicketStatus.Lukket);
    }

    [Theory]
    [InlineData(TicketStatus.Ny, TicketStatus.Lukket)]
    [InlineData(TicketStatus.Ny, TicketStatus.ÅbenTildelt)]
    [InlineData(TicketStatus.UnderAiAnalyse, TicketStatus.AfventerKunde)]
    public void Transition_TilUgyldigStatus_Kaster(TicketStatus from, TicketStatus to)
    {
        var ticket = NewTicket();
        // bring til 'from'-tilstand hvis den ikke er start
        if (from == TicketStatus.UnderAiAnalyse)
            ticket.Transition(TicketStatus.UnderAiAnalyse);

        var act = () => ticket.Transition(to);

        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void Lukket_ErTerminal()
    {
        var ticket = NewTicket();
        ticket.Transition(TicketStatus.UnderAiAnalyse);
        ticket.Transition(TicketStatus.Eskaleret);
        ticket.Transition(TicketStatus.ÅbenTildelt);
        ticket.Transition(TicketStatus.Lukket);

        ticket.CanTransitionTo(TicketStatus.ÅbenTildelt).Should().BeFalse();
    }
}
