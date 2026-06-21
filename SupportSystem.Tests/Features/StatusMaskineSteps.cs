using FluentAssertions;
using Reqnroll;
using SupportSystem.Domain;

namespace SupportSystem.Tests.Features;

[Binding]
public class StatusMaskineSteps
{
    private Ticket _ticket = default!;
    private Exception? _exception;

    [Given("en ny billet")]
    public void GivetEnNyBillet() => _ticket = new Ticket
    {
        TicketNumber = "TKT-2026-00001",
        CustomerName = "Test Kunde",
        CustomerEmail = "test@example.dk",
        Subject = "Emne",
        Description = "Beskrivelse",
    };

    [When("billetten gennemgår overgangene")]
    public void NårBillettenGennemgårOvergangene(DataTable overgange)
    {
        foreach (var row in overgange.Rows)
            _ticket.Transition(Enum.Parse<TicketStatus>(row["status"]));
    }

    [When("jeg forsøger at skifte til {string}")]
    public void NårJegForsøgerAtSkifteTil(string status) =>
        _exception = Record.Exception(() => _ticket.Transition(Enum.Parse<TicketStatus>(status)));

    [Then("er billettens status {string}")]
    public void SåErBillettensStatus(string status) =>
        _ticket.Status.Should().Be(Enum.Parse<TicketStatus>(status));

    [Then("afvises overgangen som ugyldig")]
    public void SåAfvisesOvergangenSomUgyldig() =>
        _exception.Should().BeOfType<InvalidStatusTransitionException>();
}
