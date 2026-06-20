# Datamodel

## Entitetsdiagram (forenklet)

```
Ticket 1 ──── 1 AiAnalysis
Ticket 1 ──── N Message
Ticket 1 ──── N Note
Ticket N ──── 1 Agent     (AssignedToAgent)
Ticket N ──── 1 Category  (CategoryId, rigtig FK)
AiAnalysis N ──── N KnowledgeArticle  (SourceArticles — EF skip-navigation, implicit join-tabel)
```

## Entiteter

### Ticket

| Felt | Type | Beskrivelse |
|---|---|---|
| Id | Guid | Primærnøgle |
| TicketNumber | string | Menneskelig reference, f.eks. `TKT-2026-00042` |
| CustomerName | string | Kundens navn |
| CustomerEmail | string | Kundens email |
| Subject | string | Emne, max 200 tegn |
| Description | string | Fritekstbeskrivelse, max 5000 tegn |
| Status | TicketStatus | Aktuel status (se enum) |
| Priority | Priority | Prioritet (se enum) |
| CategoryId | Guid? | **FK til Category** (nullable indtil klassificeret/bekræftet) |
| AssignedToAgentId | Guid? | Tildelt agent (nullable) |
| AssignedToTeam | string? | Tildelt team (nullable) |
| LockedByAgentId | Guid? | Agent der har låsen (nullable) |
| LockedUntil | DateTime? | Udløbstidspunkt for lås (nullable) |
| SlaDeadline | DateTime? | Deadline for løsning |
| SlaBreach | bool | True hvis SLA er brudt |
| ResolutionMinutes | int? | Samlet løsningstid i minutter |
| CreatedAt | DateTime | Oprettelsestidspunkt |
| UpdatedAt | DateTime | Senest opdateret |
| ClosedAt | DateTime? | Lukketidspunkt (nullable) |
| ClosureReason | ClosureReason? | Årsag til lukning (nullable) |

> **Bemærk:** `Category` er nu en rigtig FK (`CategoryId`), ikke et kategorinavn. Dermed kan kategorier omdøbes frit uden at røre billetter. `Ticket.CategoryId` er den *operationelle* kategori (admin-styret tabel); AI'ens rå klassifikations-forslag bor separat på `AiAnalysis.SuggestedCategory`.

### AiAnalysis

| Felt | Type | Beskrivelse |
|---|---|---|
| Id | Guid | Primærnøgle |
| TicketId | Guid | FK til Ticket (1:1) |
| SuggestedCategory | AiCategory | AI'ens rå kategori-forslag (fast enum, se ai-pipeline.md) |
| CategoryConfidence | decimal | Konfidensscore 0.00–1.00 |
| Sentiment | Sentiment | Detekteret sentiment (se enum) |
| SuggestedReply | string | AI-genereret svarforslag |
| SourceArticles | List\<KnowledgeArticle\> | Navigation — artikler brugt i RAG (implicit join-tabel) |
| GenerationMs | int | Genereringstid i millisekunder |
| Feedback | AiFeedback? | Agent-feedback på forslaget (nullable) |
| FeedbackCharDiff | int? | Antal ændrede tegn ved redigering (nullable) |
| CreatedAt | DateTime | Tidspunkt for AI-analyse |

> AI'ens forslag mappes i pipelinen til en seeded `Category` og sætter `Ticket.CategoryId`. `SuggestedCategory` bevares som det modellen rent faktisk svarede — vigtigt for feedback-loop og rapporter, da agentens override kan afvige.

### Message

| Felt | Type | Beskrivelse |
|---|---|---|
| Id | Guid | Primærnøgle |
| TicketId | Guid | FK til Ticket |
| Direction | Direction | `Indgående` (kunde) eller `Udgående` (agent) |
| Body | string | Beskedindhold |
| SentByAgentId | Guid? | Agent der sendte (nullable ved indgående) |
| IsAiGenerated | bool | True hvis besked er et accepteret AI-forslag |
| CreatedAt | DateTime | Afsendelsestidspunkt |

### Note (intern)

| Felt | Type | Beskrivelse |
|---|---|---|
| Id | Guid | Primærnøgle |
| TicketId | Guid | FK til Ticket |
| Body | string | Noteindhold (vises kun for agenter) |
| CreatedByAgentId | Guid | Agent der oprettede noten |
| CreatedAt | DateTime | Oprettelsestidspunkt |

### KnowledgeArticle

| Felt | Type | Beskrivelse |
|---|---|---|
| Id | Guid | Primærnøgle |
| Title | string | Artiklens titel |
| Content | string | Indhold i Markdown-format |
| Tags | List\<string\> | Kategori-tags til filtrering |
| Embedding | VECTOR(1536)? | Embedding af Title+Content (SQL Server `VECTOR`-kolonne, nullable indtil beregnet) |
| IsActive | bool | Inaktive artikler udelukkes fra RAG |
| TimesUsedInRag | int | Tæller — inkrementeres ved hvert RAG-hit |
| CreatedAt | DateTime | Oprettelsestidspunkt |
| UpdatedAt | DateTime | Senest opdateret |

> `Embedding` (re)beregnes af et Hangfire-job ved oprettelse/opdatering af artiklen. Vektorstørrelsen 1536 matcher `text-embedding-3-small`; justér hvis embedding-model ændres.

### Agent

| Felt | Type | Beskrivelse |
|---|---|---|
| Id | Guid | Primærnøgle |
| Name | string | Fulde navn |
| Email | string | Email (bruges til login) |
| Team | string? | Teamnavn (nullable) |
| IsActive | bool | Inaktive agenter vises ikke til tildeling |

### Category

| Felt | Type | Beskrivelse |
|---|---|---|
| Id | Guid | Primærnøgle |
| Name | string | Kategorinavn (kan omdøbes frit — billetter peger på Id) |
| IsActive | bool | Inaktive kategorier bruges ikke til nye billetter |
| SortOrder | int | Visningsrækkefølge |

> Seedes med de fem `AiCategory`-værdier som udgangspunkt; admin kan tilføje flere operationelle kategorier ovenpå.

## Enums

```csharp
enum TicketStatus
{
    Ny,
    UnderAiAnalyse,
    ÅbenUtildelt,
    ÅbenTildelt,
    AfventerKunde,
    Eskaleret,
    Lukket
}

enum Priority
{
    Lav,
    Normal,
    Høj,
    Kritisk
}

enum Sentiment
{
    Positiv,
    Neutral,
    Negativ,
    TydeligFrustreret
}

// AI'ens faste klassifikations-skema (matcher prompt-output, se ai-pipeline.md)
enum AiCategory
{
    Fakturering,
    TekniskFejl,
    Konto,
    Generelt,
    Ukendt
}

enum Direction
{
    Indgående,
    Udgående
}

enum ClosureReason
{
    Løst,
    Duplikat,
    KundeIkkeNåBare,
    UdenForScope,
    LuketAfKunde
}

enum AiFeedback
{
    Accepteret,
    RedigeretOgSendt,
    Afvist
}
```
