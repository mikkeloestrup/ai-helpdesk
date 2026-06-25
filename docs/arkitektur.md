# Arkitektur

## Løsningsstruktur (simpel, feature-orienteret)

Projektet er bevidst holdt simpelt: **ét app-projekt** organiseret efter feature (vertical slices) i stedet for tekniske lag. Aspire tilføjer kun orkestrerings-scaffolding (`apphost.cs` + `ServiceDefaults`) — det er ikke en genindførelse af lag-arkitektur.

```
ai-helpdesk/
├── apphost.cs                      # Single-file Aspire AppHost (IKKE et projekt)
├── SupportSystem.ServiceDefaults/  # OpenTelemetry, health checks, resilience (delt projekt)
├── SupportSystem/                  # Selve appen — ét projekt
│   ├── Domain/                     # Entiteter, enums, forretningslogik (status-maskine, kalkulatorer)
│   ├── Features/                   # Vertical slices: endpoint + handler + validator + DTOs pr. use case
│   │   ├── Tickets/
│   │   ├── Agents/
│   │   └── Admin/
│   ├── Data/                       # SupportDbContext, EF-konfigurationer, migrations
│   ├── Ai/                         # Embedding, LLM-kald, pipeline-job, prompt-bygning
│   └── Program.cs                  # DI, middleware, endpoint-registrering
└── SupportSystem.Tests/            # xUnit
```

## Designprincipper

- **Ingen mediator, ingen bus.** Use cases er almindelige handler-/service-klasser der injectes direkte. Ingen MediatR.
- **Rich domain model.** Forretningslogik (status-maskine, prioritets- og SLA-beregning, invarianter) bor som metoder på entiteterne i `Domain/`, ikke i handlers. Det holder logikken testbar uden infrastruktur.
- **Async arbejde via Hangfire.** AI-analyse, embedding-opdatering, SLA-breach og lås-oprydning kører som Hangfire-jobs på SQL Server-storage — ikke som in-process events.
- **Ét datalager.** SQL Server 2025 rummer både relationelle data og vektorer (native `VECTOR`-type). Ingen separat vector-database.

## Teknologivalg

| Teknologi | Valg | Begrundelse |
|---|---|---|
| Orkestrering | .NET Aspire (single-file `apphost.cs`) | Starter SQL Server, injecter connection strings, dashboard med logs/traces/metrics |
| Web framework | ASP.NET Core Minimal API | Letvægts, moderne .NET tilgang |
| ORM | Entity Framework Core | Standard i .NET-økosystemet |
| Database | SQL Server 2025 (dev + prod) | Native `VECTOR`-type → data og embeddings ét sted; LocalDB/container i dev |
| Vektorsøgning | SQL Server `VECTOR_DISTANCE` | Indbygget cosine-søgning — ingen ekstern vector-DB |
| Validering | FluentValidation | Deklarativ; kaldes via endpoint-filter |
| AI-klient | Microsoft.Extensions.AI | `IChatClient` + `IEmbeddingGenerator` — provider vælges pr. miljø, uden Semantic Kernel |
| Chat-model | Ollama/Gemma 3 12B (dev) · Claude (prod) | Stateless → må variere pr. miljø; se ai-pipeline.md |
| Embedding-model | bge-m3 (1024 dim, self-hosted) | Multilingual; **identisk i dev og prod** (delt vektorrum) |
| Baggrundsjobs | Hangfire (SQL Server storage) | Retries, dashboard, transaktionel enqueue sammen med EF |
| Logging | Microsoft.Extensions.Logging + OpenTelemetry | Struktureret logging via built-in + OTel; vist i Aspire-dashboard |
| Test | xUnit + Reqnroll + FluentAssertions + Bogus | Unit + BDD (Gherkin) teststack |

## Kommunikationsflow

```
HTTP Request
    │
    ▼
Minimal API Endpoint  ──►  FluentValidation (endpoint-filter)
    │
    ▼
Feature Handler (almindelig klasse, injectet)
    │
    ├──► Domain-logik (entitetsmetoder)
    ├──► SupportDbContext (EF Core)
    │
    └──► (ved oprettelse) BackgroundJob.Enqueue → Hangfire
                              │
                              ▼
                    TicketAiAnalysisJob
                      embedding → vektorsøgning (SQL) → Claude →
                      persistér AiAnalysis → prioritet → SLA → status
```

`POST /api/tickets` opretter billetten, enqueuer AI-jobbet i **samme transaktion** som EF-gemningen (Hangfire SQL-storage), og returnerer `201` med det samme. Et recurring sikkerhedsnet-job re-enqueuer billetter der hænger i `Ny`/`UnderAiAnalyse` (i tilfælde af crash mellem gem og enqueue).
