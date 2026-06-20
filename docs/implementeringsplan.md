# Implementeringsplan

## NuGet-pakker

| Pakke | Projekt | Formål |
|---|---|---|
| `Aspire.Hosting.AppHost` | apphost.cs | Aspire-orkestrering (single-file AppHost) |
| `Aspire.Hosting.SqlServer` | apphost.cs | SQL Server 2025-resource |
| `Microsoft.Extensions.AI` | SupportSystem | `IChatClient` (Claude) + `IEmbeddingGenerator` |
| `Microsoft.EntityFrameworkCore.SqlServer` | SupportSystem | ORM + SQL Server 2025 (inkl. `VECTOR`-type) |
| `Hangfire.AspNetCore` | SupportSystem | Baggrundsjobs (AI-analyse, embedding, SLA-breach, lås-oprydning) |
| `Hangfire.SqlServer` | SupportSystem | Hangfire-storage på SQL Server |
| `FluentValidation` | SupportSystem | Request-validering (via endpoint-filter) |
| `Swashbuckle.AspNetCore` | SupportSystem | Swagger/OpenAPI dokumentation |
| `xUnit` | Tests | Test framework |
| `FluentAssertions` | Tests | Læsbare assertions |
| `Bogus` | Tests | Realistisk testdatagenerering |

> Logging dækkes af built-in `Microsoft.Extensions.Logging` + OpenTelemetry fra `ServiceDefaults` (vist i Aspire-dashboardet) — ingen Serilog. AI-orkestrering klares direkte med `Microsoft.Extensions.AI` — ingen Semantic Kernel. Vektorsøgning sker i SQL Server — ingen Qdrant.

## Fase 1 — Fundament

**Mål:** Kørende app via Aspire med basis CRUD og status-maskine
**Estimat:** 1–2 uger

### Opgaver

1. **Opret struktur**
   ```bash
   dotnet new web -n SupportSystem
   dotnet new aspire-servicedefaults -n SupportSystem.ServiceDefaults
   dotnet new xunit -n SupportSystem.Tests
   # apphost.cs oprettes som single-file AppHost i repo-roden (ikke et projekt)
   ```

2. **Domain**
   - Opret entiteter: `Ticket`, `Message`, `Note`, `Agent`, `Category`, `KnowledgeArticle`, `AiAnalysis`
   - Definer alle enums (inkl. `AiCategory`)
   - Implementér status-maskine som domænemetode: `Ticket.Transition(TicketStatus newStatus)`

3. **Aspire + EF Core setup**
   - `apphost.cs`: `AddSqlServer("sql").WithImageTag("2025-latest")` + `AddDatabase("supportdb")`
   - `SupportDbContext` med alle entiteter; fluent config (indeks, constraints, enum-conversions, `VECTOR`-kolonne, implicit join AiAnalysis↔KnowledgeArticle)
   - Initial migration
   - Seed: 3 agenter, 4 kategorier (matcher `AiCategory`)

4. **Basis CRUD (feature-handlers)**
   - `CreateTicket` handler + `POST /api/tickets`
   - `GetTicketStatus` handler + `GET /api/tickets/{id}/status`

5. **Unit tests**
   - Status-maskine: alle gyldige og ugyldige overgange
   - Valideringsregler: emne og beskrivelse

---

## Fase 2 — Forretningslogik

**Mål:** Komplet agentworkflow med prioritet, SLA og locking
**Estimat:** 1–2 uger

6. **Prioritetsberegning** — `PriorityCalculator` i Domain + unit tests
7. **SLA-beregning** — `SlaCalculator` med arbejdstimer-logik + dansk helligdagskalender (hardcoded for indeværende år er fint)
8. **Agentendpoints** — queue, åbn (med locking), reply, keepalive, assign, close, notes
9. **Locking-mekanisme** — Hangfire-job der rydder udløbne låse hvert minut
10. **Kategoriadministration** — `GET/POST/PUT /api/admin/categories`

---

## Fase 3 — AI-integration

**Mål:** Fungerende RAG-pipeline med automatisk analyse
**Estimat:** 1–2 uger

11. **Vektorlager** — `VECTOR(1536)`-kolonne på `KnowledgeArticle`; verificér `VECTOR_DISTANCE` virker mod SQL Server 2025
12. **Videnbase-administration** — `GET/POST/PUT/DELETE /api/admin/knowledge-articles`; ved upsert enqueues embedding-job
13. **Embedding-service** — `IEmbeddingGenerator` via `Microsoft.Extensions.AI`; Hangfire-job beregner og gemmer embedding
14. **RAG-søgning** — SQL-query med `VECTOR_DISTANCE`, filtrér similarity ≥ 0.70, top-3
15. **AI-analysejob** — `TicketAiAnalysisJob` (Hangfire); fuld pipeline (se ai-pipeline.md); structured output med error handling og retry
16. **Sentimenteskalering** — som del af AI-jobbet; opret automatisk intern note ved eskalering

---

## Fase 4 — Rapportering og polish

**Mål:** Rapporter, SLA-breach job og integration tests
**Estimat:** 1 uge

17. **Feedback-loop persistering** — valider at `aiFeedback` altid sendes med reply-request; aggregeringslogik
18. **Rapporterings-endpoint** — `GET /api/admin/reports/overview` med effektive SQL-aggregeringer
19. **SLA-breach baggrundsjob** — Hangfire recurring job hvert 5. minut; markér overskredne billetter
20. **Sikkerhedsnet-job** — recurring job der re-enqueuer billetter hængt i `Ny`/`UnderAiAnalyse`
21. **Integration tests** — fuld opret-og-analyser flow med mock `IChatClient`; SLA-beregning; locking-scenarie (to agenter, samme billet)
22. **Swagger/OpenAPI** — XML-kommentarer, eksempel-requests og responses

---

## Lokal udviklingsopsætning

```bash
# Klon repo
git clone https://github.com/mikkeloestrup/ai-helpdesk.git
cd ai-helpdesk

# Konfigurér AI-nøgle som Aspire-parameter (user secrets på AppHost)
dotnet user-secrets set "Parameters:ai-api-key" "din-api-nøgle"   # i apphost-konteksten

# Start alt via Aspire (starter SQL Server 2025-container, appen og dashboard)
aspire run            # eller: dotnet run apphost.cs

# Aspire-dashboard viser app-URL, logs, traces og metrics.
# App:               https://localhost:5001
# Swagger UI:        https://localhost:5001/swagger
# Hangfire dashboard:https://localhost:5001/hangfire
```

> Migrations: kør `dotnet ef database update` mod `SupportSystem`-projektet, eller kald `db.Database.Migrate()` ved opstart i dev. SQL Server-containeren styres af Aspire — ingen manuel `docker run`.
