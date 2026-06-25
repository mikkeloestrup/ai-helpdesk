using Hangfire;
using Microsoft.EntityFrameworkCore;
using SupportSystem.Ai;
using SupportSystem.Data;
using SupportSystem.Features.Admin;
using SupportSystem.Features.Agents;
using SupportSystem.Features.Tickets;

var builder = WebApplication.CreateBuilder(args);

// Aspire: OpenTelemetry, health checks, resilience
builder.AddServiceDefaults();

// Data: SQL Server 2025 (connection injectet af Aspire)
builder.AddSqlServerDbContext<SupportDbContext>("supportdb");

// AI: chat (Ollama dev / Claude prod) + embedding (bge-m3, identisk i alle miljøer)
builder.AddAppAiClients();

// Baggrundsjobs på SQL Server-storage
builder.Services.AddHangfire((sp, config) => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("supportdb")));
builder.Services.AddHangfireServer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHangfireDashboard("/hangfire");

app.MapTicketEndpoints()
   .MapAgentEndpoints()
   .MapAdminEndpoints();

// I dev: kør migrations og seed automatisk
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SupportDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.Run();
