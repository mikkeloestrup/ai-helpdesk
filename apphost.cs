#:sdk Aspire.AppHost.Sdk@13.4.6
#:package Aspire.Hosting.SqlServer@13.4.6
#:package CommunityToolkit.Aspire.Hosting.Ollama@13.4.0
#:project ./SupportSystem/SupportSystem.csproj

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// SQL Server 2025 — relationelle data + native VECTOR-type (data og vektorer ét sted)
var sql = builder.AddSqlServer("sql")
    .WithImageTag("2025-latest")
    .WithDataVolume();

var db = sql.AddDatabase("supportdb");

var api = builder.AddProject<Projects.SupportSystem>("api")
    .WithReference(db)
    .WaitFor(db);

if (builder.Environment.IsDevelopment())
{
    // Lokal LLM i dev: Gemma 3 12B (chat) + bge-m3 (embedding) via Ollama på GPU
    var ollama = builder.AddOllama("ollama")
        .WithDataVolume()
        .WithGPUSupport();

    var chat = ollama.AddModel("chat", "gemma3:12b");
    var embedding = ollama.AddModel("embedding", "bge-m3");

    api.WithReference(chat).WaitFor(chat)
       .WithReference(embedding).WaitFor(embedding);
}
else
{
    // Prod-chat: Claude. Embedding (bge-m3) skal være self-hosted og IDENTISK med dev.
    var anthropicApiKey = builder.AddParameter("anthropic-api-key", secret: true);
    api.WithEnvironment("Anthropic__ApiKey", anthropicApiKey);
}

builder.Build().Run();
