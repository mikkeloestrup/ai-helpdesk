using Anthropic.SDK;

namespace SupportSystem.Ai;

/// <summary>
/// Registrerer AI-providere pr. miljø jf. ai-pipeline.md "Model-opsætning".
/// Chat må variere (stateless); embedding-modellen (bge-m3) skal være IDENTISK i dev og prod.
/// </summary>
public static class AiClientRegistration
{
    public const string ChatModelConnection = "chat";
    public const string EmbeddingModelConnection = "embedding";

    public static void AddAppAiClients(this IHostApplicationBuilder builder)
    {
        // Embedding: bge-m3 via self-hosted Ollama — samme i alle miljøer (delt vektorrum).
        builder.AddOllamaApiClient(EmbeddingModelConnection)
            .AddEmbeddingGenerator();

        if (builder.Environment.IsDevelopment())
        {
            // Dev-chat: Gemma 3 12B via Ollama.
            builder.AddOllamaApiClient(ChatModelConnection)
                .AddChatClient();
        }
        else
        {
            // Prod-chat: Claude.
            var apiKey = builder.Configuration["Anthropic:ApiKey"]
                ?? throw new InvalidOperationException("Anthropic:ApiKey mangler i prod-konfiguration.");

            builder.Services.AddChatClient(new AnthropicClient(apiKey).Messages);
        }
    }
}
