namespace SupportSystem.Features.Agents;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        // TODO US-08..US-11: queue, open+lock, reply, keepalive, assign, close, notes
        return app;
    }
}
