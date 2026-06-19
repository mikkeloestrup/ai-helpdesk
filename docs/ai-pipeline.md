# AI Pipeline

## Overblik

AI-analysen køres asynkront som en MediatR event handler, der trigges når en billet oprettes.  
Hele pipelinen skal færdiggøres inden for **45 sekunder** — ellers sættes billetten til manuel behandling.

## Sekvens

```
POST /api/tickets
    │
    ├─► Opret Ticket (status: Ny)
    └─► Udgiv TicketCreatedEvent (MediatR)
              │
              ▼
    TicketAiAnalysisHandler
              │
              ├─► Sæt status → UnderAiAnalyse
              │
              ├─► [EMBEDDING]
              │   Embed Subject + Description
              │   via Semantic Kernel text embedding
              │
              ├─► [VECTOR SEARCH]
              │   Søg Qdrant — top-3 artikler
              │   med similarity score >= 0.70
              │   (ingen hits → brug tom kontekst)
              │
              ├─► [PROMPT BYGNING]
              │   System prompt + artikel-kontekst + billet-tekst
              │
              ├─► [LLM KALD]
              │   Kald Claude via Semantic Kernel
              │   Structured output (se schema nedenfor)
              │
              ├─► [PERSISTERING]
              │   Gem AiAnalysis med alle felter
              │   Inkrementer TimesUsedInRag på brugte artikler
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

LLM instrueres til at returnere præcis dette JSON-format.  
Brug Semantic Kernels structured output eller `JsonSchemaExporter` til at håndhæve det.

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

Gyldige værdier for `category`: `Fakturering`, `TekniskFejl`, `Konto`, `Generelt`, `Ukendt`  
Gyldige værdier for `sentiment`: `Positiv`, `Neutral`, `Negativ`, `TydeligFrustreret`

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

- **Embedding model:** `text-embedding-3-small` (OpenAI) eller tilsvarende via Semantic Kernel
- **Vector DB:** Qdrant, collection: `knowledge-articles`
- **Søgestrategi:** Cosine similarity, top-3 resultater
- **Minimumsscore:** 0.70 — artikler under denne tærskel udelukkes
- **Fallback:** Hvis ingen artikler rammer tærsklen, genereres svar uden kontekst

## Fejlhåndtering

| Situation | Håndtering |
|---|---|
| LLM timeout (> 45 sek) | Status → `ÅbenUtildelt`, prioritet → `Høj`, intern note om timeout |
| Ugyldig JSON fra LLM | Retry én gang, derefter fallback til manuel behandling |
| Qdrant utilgængelig | Spring RAG over, kald LLM uden kontekst |
| Konfidensscore < 0.6 | Kategori → `Ukendt`, prioritet → `Høj` |

## Feedback-loop

Agentens feedback på AI-forslaget gemmes på `AiAnalysis`:

| Handling | AiFeedback | FeedbackCharDiff |
|---|---|---|
| Agent accepterer forslaget direkte | `Accepteret` | null |
| Agent redigerer og sender | `RedigeretOgSendt` | Antal ændrede tegn |
| Agent afviser og skriver selv | `Afvist` | null |

Feedback aggregeres i rapporteringsmodulet og kan bruges til at vurdere om system prompt eller RAG-konfiguration skal justeres.
