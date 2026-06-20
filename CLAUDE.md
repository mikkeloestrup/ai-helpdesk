# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Current state

This repo currently contains **only documentation** — no source code, solution, or projects exist yet. The `docs/` folder is the authoritative specification for a .NET learning project (`SupportSystem`) that has not been scaffolded. Before writing code, read the relevant doc(s); they define the contracts, data model, and business rules the implementation must match. The docs are written in **Danish** — domain terms (statuses, enums, categories) are Danish identifiers and must be kept verbatim in code (e.g. `TicketStatus.ÅbenUtildelt`, `Sentiment.TydeligFrustreret`).

Documentation map (`docs/`):
- `arkitektur.md` — structure, design principles, technology choices, communication flow
- `datamodel.md` — entities, relationships, all enums (the source of truth for enum members)
- `api-kontrakter.md` — every endpoint with request/response JSON and status codes
- `forretningsregler.md` — status machine, priority rules, SLA, locking, escalation
- `ai-pipeline.md` — RAG/AI analysis pipeline, structured-output schema, error handling
- `implementeringsplan.md` — phased build order, NuGet packages, dev setup commands

## Architecture

Deliberately **simple and feature-oriented — not Clean Architecture, no mediator, no message bus.** One application project organized by feature (vertical slices); Aspire adds only orchestration scaffolding.

```
apphost.cs                      # Single-file Aspire AppHost — NOT a .csproj project
SupportSystem.ServiceDefaults/  # OpenTelemetry, health checks, resilience (shared project)
SupportSystem/                  # The app — one project
  ├── Domain/                   # Entities, enums, business logic (status machine, calculators)
  ├── Features/{Tickets,Agents,Admin}/  # Vertical slices: endpoint + handler + validator + DTOs
  ├── Data/                     # SupportDbContext, EF configs, migrations
  ├── Ai/                       # Embedding, LLM call, pipeline job, prompt building
  └── Program.cs
SupportSystem.Tests/
```

Key principles:
- **No mediator/bus.** Use cases are plain handler/service classes injected directly. No MediatR.
- **Rich domain model.** Business logic (status machine, priority/SLA calculation, invariants) lives as methods on entities in `Domain/`, not in handlers — keeps it testable without infrastructure.
- **Async work via Hangfire** (SQL Server storage): AI analysis, embedding refresh, SLA-breach, lock cleanup. Not in-process events.
- **One datastore.** SQL Server 2025 holds both relational data and vectors (native `VECTOR` type) — no separate vector DB.
- **`apphost.cs` is a single file**, not a project. Run with `aspire run` / `dotnet run apphost.cs`.

Request flow: endpoint → FluentValidation (endpoint filter) → feature handler → domain logic + `SupportDbContext`. On ticket creation the handler **enqueues a Hangfire job in the same transaction** as the EF save and returns `201` immediately — the AI pipeline runs in that background job, not inline.

## Key cross-cutting behaviors

These span multiple files and are easy to get wrong — consult the named doc:

- **Status machine** (`forretningsregler.md`): implement transitions as a domain method on `Ticket` (e.g. `Ticket.Transition(TicketStatus)`). Invalid transitions must surface as `422 Unprocessable Entity`.
- **Priority** is computed after AI analysis, rules evaluated in order, first match wins; `TydeligFrustreret` sentiment forces `Kritisk` and escalation. Agents can manually override.
- **SLA** is measured in Danish business hours (08:00–17:00 Mon–Fri, holidays excluded). `SlaDeadline` set at AI-analysis completion; a Hangfire job sets `SlaBreach` every 5 min.
- **Locking**: opening a ticket sets `LockedByAgentId` + `LockedUntil = now + 10min`; a second agent gets `423 Locked`. Renewed via `keepalive`, released on reply/close/timeout; a Hangfire job clears expired locks each minute.
- **AI pipeline** (`ai-pipeline.md`) runs as Hangfire job `TicketAiAnalysisJob`: embedding (`IEmbeddingGenerator`) → SQL Server vector search (`VECTOR_DISTANCE('cosine', …)`, similarity ≥ 0.70 i.e. distance ≤ 0.30, top-3, empty context if none) → Claude via `IChatClient` with enforced JSON structured output → persist `AiAnalysis` → map suggested category → priority → SLA → status. 45s budget. Handle: LLM timeout, invalid JSON (retry once then manual), vector query fails (skip RAG), confidence < 0.6 (category → `Ukendt`, priority → `Høj`).
- **Two category concepts** (don't conflate — this was a fixed smell): `AiCategory` is the AI's fixed classification enum (`Fakturering`/`TekniskFejl`/`Konto`/`Generelt`/`Ukendt`), stored raw on `AiAnalysis.SuggestedCategory`. The operational category is the admin-managed `Category` **table**, referenced by `Ticket.CategoryId` (a real FK — categories rename freely). The pipeline maps the AI suggestion to a seeded `Category` row.
- **`AiAnalysis ↔ KnowledgeArticle`** is a real EF Core many-to-many via skip navigation (`AiAnalysis.SourceArticles`), implicit join table — not a serialized `List<Guid>`.
- **AI feedback loop**: every agent reply carries `aiFeedback` (`Accepteret`/`RedigeretOgSendt`/`Afvist`), persisted on `AiAnalysis` and aggregated in reports.

## Tech stack

.NET Aspire (single-file `apphost.cs`) · ASP.NET Core Minimal API · EF Core + SQL Server 2025 (data **and** vectors) · Hangfire (SQL Server storage) · Microsoft.Extensions.AI (`IChatClient` → Claude, `IEmbeddingGenerator`) · FluentValidation · built-in `Microsoft.Extensions.Logging` + OpenTelemetry (via ServiceDefaults, shown in Aspire dashboard) · xUnit + FluentAssertions + Bogus. **Not used:** MediatR, Semantic Kernel, Qdrant, Serilog, SQLite, Clean Architecture layering.

## Commands

The solution does not exist yet; scaffold per `implementeringsplan.md`. Once it exists:

```bash
# Run everything via Aspire (starts SQL Server 2025 container, app, dashboard)
aspire run            # or: dotnet run apphost.cs
# App https://localhost:5001 — Swagger at /swagger, Hangfire at /hangfire

# Build / test
dotnet build
dotnet test
dotnet test SupportSystem.Tests
dotnet test --filter "FullyQualifiedName~StatusMachine"   # single test/class

# EF Core migrations (against the SupportSystem project)
dotnet ef migrations add <Name> --project SupportSystem
dotnet ef database update

# AI key as Aspire parameter (user-secrets in the apphost context)
dotnet user-secrets set "Parameters:ai-api-key" "<key>"
```

## Conventions

- Commit messages are written in Danish (see git history, e.g. `docs: tilføj datamodel-dokumentation`).
- Keep Danish domain identifiers exactly as spelled in `datamodel.md` and `ai-pipeline.md` — including non-ASCII (`Å`, `ø`, `å`) and the existing spellings in the `ClosureReason` enum.
- `AiCategory` (AI schema enum) and the `Category` table are distinct by design — see the cross-cutting note above before touching either.
