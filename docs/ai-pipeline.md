# AI Pipeline

## Overblik

AI-analysen køres asynkront som et **Hangfire-job**, der enqueues når en billet oprettes.
`POST /api/tickets` enqueuer jobbet i samme transaktion som EF-gemningen og returnerer `201` med det samme.
Hele pipelinen skal færdiggøres inden for **45 sekunder** — ellers sættes billetten til manuel behandling.

## Sekvens

```
POST /api/tickets
    │
    ├─► Opret Ticket (status: Ny) + gem
    └─► BackgroundJob.Enqueue<TicketAiAnalysisJob>(ticketId)   (samme transaktion)
              │
              ▼
    TicketAiAnalysisJob
              │
              ├─► Sæt status → UnderAiAnalyse
              │
              ├─► [EMBEDDING]
              │   Embed Subject + Description via IEmbeddingGenerator
              │   (bge-m3, 1024 dim — samme model i dev og prod)
              │
              ├─► [VEKTORSØGNING — SQL Server]
              │   SELECT TOP 3 … VECTOR_DISTANCE('cosine', Embedding, @query) AS Distance
              │   FROM KnowledgeArticles WHERE IsActive = 1 ORDER BY Distance
              │   Behold kun hits med similarity >= 0.70  (cosine-distance <= 0.30)
              │   (ingen hits → tom kontekst)
              │
              ├─► [PROMPT BYGNING]
              │   System prompt + artikel-kontekst + billet-tekst
              │
              ├─► [LLM KALD]
              │   IChatClient → chat-model, structured output (se schema nedenfor)
              │   (dev: Gemma 3 12B via Ollama · prod: Claude — se Model-opsætning)
              │
              ├─► [PERSISTERING]
              │   Gem AiAnalysis (inkl. SourceArticles-navigation)
              │   Inkrementer TimesUsedInRag på brugte artikler
              │   Map SuggestedCategory → Category → sæt Ticket.CategoryId
              │
              ├─► [PRIORITETSBEREGNING]
              │   Anvend forretningsregler (se forretningsregler.md)
              │
              ├─► [SLA-BEREGNING]
              │   Beregn SlaDeadline ud fra prioritet og arbejdstimer
              │
              └─► Sæt status → ÅbenUtildelt (eller Eskaleret)
```

## Structured Output Schema

LLM instrueres til at returnere præcis dette JSON-format. Brug `Microsoft.Extensions.AI`'s structured output (`ChatOptions` med JSON-schema) til at håndhæve det.

```json
{
  "category": "Konto",
  "categoryConfidence": 0.87,
  "sentiment": "Negativ",
  "suggestedReply": "Hej [KundeNavn], tak for din henvendelse. Du kan nulstille din adgangskode ved at...",
  "sourceArticleIds": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "7bc91a23-4812-4321-a9fd-1b2c3d4e5f67"
  ]
}
```

Gyldige værdier for `category` (= `AiCategory`-enum): `Fakturering`, `TekniskFejl`, `Konto`, `Generelt`, `Ukendt`
Gyldige værdier for `sentiment`: `Positiv`, `Neutral`, `Negativ`, `TydeligFrustreret`

`category` er AI'ens faste klassifikations-skema og er adskilt fra `Category`-tabellen. I persisterings-trinet gemmes værdien som `AiAnalysis.SuggestedCategory` og mappes til en seeded `Category`-række, hvis Id sættes på `Ticket.CategoryId`.

## System Prompt (skabelon)

```
Du er en supportassistent der analyserer kundehenvendelser.

Du skal returnere et JSON-objekt med følgende felter:
- category: én af [Fakturering, TekniskFejl, Konto, Generelt, Ukendt]
- categoryConfidence: decimal mellem 0.0 og 1.0
- sentiment: én af [Positiv, Neutral, Negativ, TydeligFrustreret]
- suggestedReply: et venligt og præcist svar på maks. 500 ord på dansk
- sourceArticleIds: liste af artikel-ID'er du har brugt (tom liste hvis ingen)

Returner KUN valid JSON — ingen forklaring, ingen markdown.

## Videnbase-kontekst
{{ARTICLES}}

## Kundehenvendelse
Emne: {{SUBJECT}}
Beskrivelse: {{DESCRIPTION}}
```

## RAG-søgning

- **Embedding-model:** `bge-m3` (1024 dim, multilingual inkl. dansk) via `IEmbeddingGenerator`
- **Lager:** SQL Server 2025 `VECTOR`-kolonne på `KnowledgeArticle.Embedding` — ingen separat vector-DB
- **Søgestrategi:** `VECTOR_DISTANCE('cosine', …)`, `ORDER BY` distance, `TOP 3`
- **Minimumsscore:** similarity 0.70 (dvs. cosine-distance ≤ 0.30) — artikler under tærsklen udelukkes
- **Fallback:** Hvis ingen artikler rammer tærsklen, genereres svar uden kontekst
- **Indeksering:** Embeddings (re)beregnes af et Hangfire-job ved oprettelse/opdatering af artikler

## Model-opsætning (lokal dev / prod)

AI-laget går gennem `Microsoft.Extensions.AI`, så provider vælges pr. miljø i DI — resten af pipelinen er uændret.

| Rolle | Dev | Prod | Skift pr. miljø? |
|---|---|---|---|
| Chat (`IChatClient`) | Gemma 3 12B via **Ollama** (`format: json`) | **Claude** (Anthropic) | Ja — stateless, sikkert at variere |
| Embedding (`IEmbeddingGenerator`) | **bge-m3** (self-hosted, Ollama) | **bge-m3** (self-hosted) | **Nej — skal være identisk** |

- **Embedding-modellen må aldrig variere mellem miljøer:** persisterede artikel-vektorer og query-vektoren skal ligge i samme vektorrum. Skifter du embedder, skal hele videnbasen re-embeddes og `VECTOR`-dimensionen ændres.
- **Structured output lokalt:** Gemma er mindre stensikker på JSON end Claude — brug Ollamas `format: json` (eller GBNF-grammar). `retry-én-gang`-reglen ovenfor er vigtigst i dev.
- **Dansk-kvalitet:** `suggestedReply` er kundevendt — test 12B-svarene specifikt. Prod kører Claude, så den primære risiko er dev-svar der ser anderledes ud end produktion.
- **VRAM (RTX 4070, 12 GB):** chat- og embedding-model deles om pladsen; Ollama swapper evt. modeller pr. kørsel. Hæv `keep_alive` eller brug en mindre chat-model hvis model-load-latency presser 45s-budgettet.

## Fejlhåndtering

| Situation | Håndtering |
|---|---|
| LLM timeout (> 45 sek) | Status → `ÅbenUtildelt`, prioritet → `Høj`, intern note om timeout |
| Ugyldig JSON fra LLM | Retry én gang, derefter fallback til manuel behandling |
| Vektorsøgning fejler | Spring RAG over (try/catch om VECTOR-query'en), kald LLM uden kontekst |
| Konfidensscore < 0.6 | Kategori → `Ukendt`, prioritet → `Høj` |

Hangfire's egne retries dækker transiente fejl i jobbet; ovenstående er de domæne-specifikke fallbacks.

## Feedback-loop

Agentens feedback på AI-forslaget gemmes på `AiAnalysis`:

| Handling | AiFeedback | FeedbackCharDiff |
|---|---|---|
| Agent accepterer forslaget direkte | `Accepteret` | null |
| Agent redigerer og sender | `RedigeretOgSendt` | Antal ændrede tegn |
| Agent afviser og skriver selv | `Afvist` | null |

Feedback aggregeres i rapporteringsmodulet og kan bruges til at vurdere om system prompt eller RAG-konfiguration skal justeres.
