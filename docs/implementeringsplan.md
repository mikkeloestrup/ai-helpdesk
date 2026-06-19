# Implementeringsplan

## NuGet-pakker

| Pakke | Projekt | Formål |
|---|---|---|
| `MediatR` | Application | CQRS commands/queries og Domain Events |
| `FluentValidation.AspNetCore` | Api | Request-validering |
| `Microsoft.SemanticKernel` | Infrastructure | AI-orkestration og RAG-pipeline |
| `Microsoft.Extensions.AI` | Infrastructure | Abstrakt AI-klientlag |
| `Microsoft.EntityFrameworkCore` | Infrastructure | ORM |
| `Microsoft.EntityFrameworkCore.Sqlite` | Infrastructure | SQLite til lokal udvikling |
| `Microsoft.EntityFrameworkCore.SqlServer` | Infrastructure | SQL Server til produktion |
| `Qdrant.Client` | Infrastructure | Vector DB-klient |
| `Hangfire.AspNetCore` | Infrastructure | Baggrundsjobs (SLA-breach, re-embedding) |
| `Serilog.AspNetCore` | Api | Struktureret logging |
| `Swashbuckle.AspNetCore` | Api | Swagger/OpenAPI dokumentation |
| `xUnit` | Tests | Test framework |
| `FluentAssertions` | Tests | Læsbare assertions |
| `Bogus` | Tests | Realistisk testdatagenerering |
| `Microsoft.EntityFrameworkCore.InMemory` | Tests | In-memory DB til unit tests |

## Fase 1 — Fundament

**Mål:** Kørende API med basis CRUD og status-maskine  
**Issues:** #1, #2  
**Estimat:** 1–2 uger

### Opgaver

1. **Opret løsningsstruktur**
   ```
   dotnet new sln -n SupportSystem
   dotnet new classlib -n SupportSystem.Domain
   dotnet new classlib -n SupportSystem.Application
   dotnet new classlib -n SupportSystem.Infrastructure
   dotnet new webapi -n SupportSystem.Api
   dotnet new xunit -n SupportSystem.Domain.Tests
   dotnet new xunit -n SupportSystem.Application.Tests
   ```

2. **Domain-lag**
   - Opret entiteter: `Ticket`, `Message`, `Note`, `Agent`, `Category`
   - Definer alle enums
   - Implementér status-maskine som domænemetode: `Ticket.Transition(TicketStatus newStatus)`
   - Opret domæne-events: `TicketCreatedEvent`

3. **EF Core setup**
   - `SupportDbContext` med alle entiteter
   - Fluent API konfigurationer (indeks, constraints, value conversions for enums)
   - Initial migration
   - Seed: 3 agenter, 4 kategorier

4. **Basis CRUD via MediatR**
   - `CreateTicketCommand` + handler
   - `GetTicketStatusQuery` + handler
   - Endpoint: `POST /api/tickets`
   - Endpoint: `GET /api/tickets/{id}/status`

5. **Unit tests**
   - Status-maskine: test alle gyldige og ugyldige overgange
   - Valideringsregler: emne og beskrivelse

---

## Fase 2 — Forretningslogik

**Mål:** Komplet agentworkflow med prioritet, SLA og locking  
**Issues:** #3, #8, #9, #10, #11, #13  
**Estimat:** 1–2 uger

### Opgaver

6. **Prioritetsberegning**
   - Implementér `PriorityCalculator` service i Domain
   - Unit tests for alle prioritetsregler

7. **SLA-beregning**
   - Implementér `SlaCalculator` med arbejdstimer-logik
   - Dansk helligdagskalender (hardcoded for indeværende år er fint til læringsformål)

8. **Agentendpoints**
   - `GET /api/agent/queue` med filtrering og sortering
   - `GET /api/agent/tickets/{id}` med locking
   - `POST /api/agent/tickets/{id}/reply`
   - `POST /api/agent/tickets/{id}/keepalive`
   - `POST /api/agent/tickets/{id}/assign`
   - `POST /api/agent/tickets/{id}/close`
   - `POST /api/agent/tickets/{id}/notes`

9. **Locking-mekanisme**
   - Hangfire-job der rydder udløbne låse hvert minut

10. **Kategoriadministration**
    - `GET/POST/PUT /api/admin/categories`

---

## Fase 3 — AI-integration

**Mål:** Fungerende RAG-pipeline med automatisk analyse  
**Issues:** #4, #5, #6, #7, #12  
**Estimat:** 1–2 uger

### Opgaver

11. **Qdrant setup**
    ```bash
    docker run -p 6333:6333 qdrant/qdrant
    ```
    - Opret collection `knowledge-articles` med korrekt vector størrelse

12. **Videnbase-administration**
    - `GET/POST/PUT/DELETE /api/admin/knowledge-articles`
    - Ved oprettelse/opdatering: trigger `KnowledgeArticleUpsertedEvent`

13. **Embedding-service**
    - `IEmbeddingService` interface i Application
    - Implementation i Infrastructure via Semantic Kernel
    - Embed og gem til Qdrant ved videnbase-ændringer

14. **RAG-søgning**
    - `IVectorSearchService` interface i Application
    - Implementation: søg Qdrant, filtrer på score >= 0.70, returnér top-3

15. **AI-analysejob**
    - `TicketAiAnalysisHandler` som MediatR notification handler
    - Implementér fuld pipeline (se ai-pipeline.md)
    - Structured output parsing med error handling og retry

16. **Sentimenteskalering**
    - Implementér som del af AI-analysejobbet
    - Opret automatisk intern note ved eskalering

---

## Fase 4 — Rapportering og polish

**Mål:** Rapporter, SLA-breach job og integration tests  
**Issues:** #14  
**Estimat:** 1 uge

### Opgaver

17. **Feedback-loop persistering**
    - Valider at `aiFeedback` altid sendes med reply-request
    - Aggregeringslogik til rapporter

18. **Rapporterings-endpoint**
    - `GET /api/admin/reports/overview`
    - Effektive SQL-aggregeringer via EF Core

19. **SLA-breach baggrundsjob**
    - Hangfire recurring job hvert 5. minut
    - Markér billetter med `SlaBreach = true` der har overskredet deadline

20. **Integration tests**
    - Test fuld opret-og-analyser flow med mock AI
    - Test SLA-beregning
    - Test locking-scenarie (to agenter, samme billet)

21. **Swagger/OpenAPI**
    - XML-kommentarer på alle endpoints
    - Eksempel-requests og responses

---

## Lokal udviklingsopsætning

```bash
# Klon repo
git clone https://github.com/mikkeloestrup/ai-helpdesk.git
cd ai-helpdesk

# Start Qdrant
docker run -d -p 6333:6333 --name qdrant qdrant/qdrant

# Konfigurér user secrets
cd src/SupportSystem.Api
dotnet user-secrets set "AI:ApiKey" "din-api-nøgle"
dotnet user-secrets set "Qdrant:Host" "localhost"

# Kør migrations
dotnet ef database update

# Start API
dotnet run
# API kører på https://localhost:5001
# Swagger UI: https://localhost:5001/swagger
# Hangfire dashboard: https://localhost:5001/hangfire
```
