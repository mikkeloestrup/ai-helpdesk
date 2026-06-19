# Arkitektur

## Løsningsstruktur (Clean Architecture)

\`\`\`
SupportSystem.sln
├── src/
│   ├── SupportSystem.Domain/          # Entiteter, enums, domænelogik, interfaces
│   ├── SupportSystem.Application/     # Use cases (CQRS commands/queries), MediatR handlers
│   ├── SupportSystem.Infrastructure/  # EF Core, Vector DB, AI-klienter, baggrundsjobs
│   └── SupportSystem.Api/             # ASP.NET Core Minimal API, endpoints, middleware
└── tests/
    ├── SupportSystem.Domain.Tests/
    ├── SupportSystem.Application.Tests/
    └── SupportSystem.Integration.Tests/
\`\`\`

## Lagoversigt

### Domain
Ingen afhængigheder til andre lag. Indeholder:
- Entiteter (`Ticket`, `AiAnalysis`, `Message`, `KnowledgeArticle`, `Agent`)
- Enums (`TicketStatus`, `Priority`, `Sentiment` m.fl.)
- Domænelogik og invarianter (status-maskine, prioritetsberegning)
- Interfaces (`ITicketRepository`, `IKnowledgeArticleRepository`)
- Domain Events (`TicketCreatedEvent`, `TicketEscalatedEvent`)

### Application
Afhænger kun af Domain. Indeholder:
- CQRS Commands og Queries (MediatR)
- Event Handlers
- Applikationsservices
- DTOs og mapping
- Valideringslogik (FluentValidation)

### Infrastructure
Afhænger af Domain og Application. Indeholder:
- EF Core DbContext og konfigurationer
- Repository-implementationer
- Qdrant Vector DB-klient og embedding-service
- AI-klienter (Semantic Kernel)
- Hangfire baggrundsjobs
- Serilog-konfiguration

### Api
Afhænger af Application (og Infrastructure for DI-registrering). Indeholder:
- Minimal API endpoint-definitioner
- Middleware (fejlhåndtering, autentifikation)
- Swagger/OpenAPI-konfiguration
- Program.cs og DI-setup

## Teknologivalg

| Teknologi | Valg | Begrundelse |
|---|---|---|
| Web framework | ASP.NET Core Minimal API | Letvægts, moderne .NET tilgang |
| ORM | Entity Framework Core | Standard i .NET-økosystemet |
| Database (dev) | SQLite | Ingen opsætning lokalt |
| Database (prod) | SQL Server / PostgreSQL | Skalérbarhed |
| Mediator | MediatR | CQRS og Domain Events |
| Validering | FluentValidation | Deklarativ og testbar |
| AI-orkestration | Semantic Kernel | Microsoft-støttet, Anthropic-kompatibel |
| Vector DB | Qdrant | Open source, god .NET-klient |
| Baggrundsjobs | Hangfire | Simpel opsætning, dashboard inkluderet |
| Logging | Serilog | Struktureret logging |
| Test | xUnit + FluentAssertions + Bogus | Standard .NET teststack |

## Kommunikationsflow

\`\`\`
HTTP Request
    │
    ▼
Minimal API Endpoint
    │
    ▼
MediatR Command/Query
    │
    ├──► Command Handler (Application)
    │         │
    │         ├──► Domain logic
    │         ├──► Repository (Infrastructure)
    │         └──► Domain Event → MediatR Notification
    │                   │
    │                   └──► Event Handler (f.eks. AI-analyse)
    │
    └──► Query Handler (Application)
              │
              └──► Repository / DbContext (read-side)
\`\`\`