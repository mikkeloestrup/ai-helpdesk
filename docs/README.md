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
- **Stack:** .NET, ASP.NET Core Minimal API, EF Core, Semantic Kernel, Qdrant
- **Arkitektur:** Clean Architecture med CQRS via MediatR
- **AI-model:** Anthropic Claude (via Semantic Kernel)
- **Vector DB:** Qdrant (lokal Docker under udvikling)