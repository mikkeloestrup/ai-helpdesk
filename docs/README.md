# Dokumentation — AI Helpdesk

Denne mappe indeholder al teknisk og forretningsmæssig dokumentation for projektet.

## Indhold

| Fil | Beskrivelse |
|---|---|
| [arkitektur.md](arkitektur.md) | Løsningsstruktur, lag-oversigt og teknologivalg |
| [datamodel.md](datamodel.md) | Entiteter, relationer og enums |
| [api-kontrakter.md](api-kontrakter.md) | Alle API-endpoints med request/response-eksempler |
| [forretningsregler.md](forretningsregler.md) | Status-maskine, prioritetsregler og SLA-definition |
| [ai-pipeline.md](ai-pipeline.md) | RAG-pipeline, prompt-design og structured output |
| [implementeringsplan.md](implementeringsplan.md) | Faser, rækkefølge og anbefalede NuGet-pakker |

## Hurtig reference

- **Repository:** mikkeloestrup/ai-helpdesk
- **Stack:** .NET Aspire, ASP.NET Core Minimal API, EF Core, Microsoft.Extensions.AI, Hangfire
- **Arkitektur:** Simpel, feature-orienteret (vertical slices) — ét app-projekt, ingen mediator/bus
- **Chat-model:** Ollama/Gemma 3 12B (dev) · Anthropic Claude (prod) — via Microsoft.Extensions.AI
- **Embedding-model:** bge-m3 (1024 dim, self-hosted) — identisk i dev og prod
- **Database:** SQL Server 2025 — relationelle data og vektorer (`VECTOR`-type) ét sted