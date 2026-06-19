# API Kontrakter

Base URL: `/api`  
Format: JSON  
Autentifikation: Bearer token (ikke implementeret i Fase 1)

---

## Kundeendpoints

### POST /api/tickets
Opret ny supportbillet.

**Request:**
```json
{
  "customerName": "Jens Jensen",
  "customerEmail": "jens@example.dk",
  "subject": "Kan ikke logge ind",
  "description": "Siden jeg opdaterede min adgangskode kan jeg ikke komme ind..."
}
```

**Response `201 Created`:**
```json
{
  "ticketId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "ticketNumber": "TKT-2026-00042",
  "status": "Ny",
  "message": "Din billet er modtaget og vil blive analyseret inden for 30 sekunder."
}
```

**Response `400 Bad Request`:**
```json
{
  "errors": {
    "subject": ["Emne er påkrævet"],
    "description": ["Beskrivelse må maks. være 5000 tegn"]
  }
}
```

---

### GET /api/tickets/{ticketId}/status
Hent billetstatus for kunde. AI-forslag vises ikke.

**Response `200 OK`:**
```json
{
  "ticketNumber": "TKT-2026-00042",
  "subject": "Kan ikke logge ind",
  "status": "AfventerKunde",
  "priority": "Høj",
  "category": "Konto",
  "lastAgentReply": "Prøv at nulstille din adgangskode via...",
  "lastReplyAt": "2026-06-19T10:30:00Z",
  "createdAt": "2026-06-19T09:00:00Z"
}
```

**Response `404 Not Found`:** Billet ikke fundet eller ældre end 90 dage.

---

### POST /api/tickets/{ticketId}/reply
Kunde svarer på billet. Skifter status fra `AfventerKunde` → `ÅbenTildelt`.

**Request:**
```json
{
  "message": "Det virkede! Tak for hjælpen."
}
```

**Response `200 OK`**

---

### POST /api/tickets/{ticketId}/close
Kunde lukker billet selv.

**Response `200 OK`**  
**Response `409 Conflict`:** Billet er allerede lukket eller annulleret.

---

## Agentendpoints

### GET /api/agent/queue
Hent prioriteret billetkø.

**Query params:**

| Param | Type | Beskrivelse |
|---|---|---|
| category | string? | Filtrer på kategori |
| priority | string? | Filtrer på prioritet |
| status | string? | Filtrer på status |
| assignedToMe | bool | Kun egne billetter |
| page | int | Sidenummer (default: 1) |
| pageSize | int | Antal pr. side (default: 25) |

**Response `200 OK`:**
```json
{
  "items": [
    {
      "ticketId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "ticketNumber": "TKT-2026-00042",
      "subject": "Kan ikke logge ind",
      "customerEmail": "jens@example.dk",
      "status": "ÅbenUtildelt",
      "priority": "Høj",
      "category": "Konto",
      "sentiment": "Negativ",
      "aiConfidence": 0.87,
      "slaDeadline": "2026-06-19T17:00:00Z",
      "slaBreach": false,
      "ageMinutes": 42
    }
  ],
  "totalCount": 17,
  "page": 1,
  "pageSize": 25
}
```

---

### GET /api/agent/tickets/{ticketId}
Åbn billet og sæt lås (10 minutter).

**Response `200 OK`:**
```json
{
  "ticketId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "ticketNumber": "TKT-2026-00042",
  "customerName": "Jens Jensen",
  "customerEmail": "jens@example.dk",
  "subject": "Kan ikke logge ind",
  "description": "Siden jeg opdaterede...",
  "status": "ÅbenUtildelt",
  "priority": "Høj",
  "category": "Konto",
  "aiAnalysis": {
    "category": "Konto",
    "categoryConfidence": 0.87,
    "sentiment": "Negativ",
    "suggestedReply": "Hej Jens, tak for din henvendelse...",
    "sourceArticles": [
      { "id": "...", "title": "Nulstil adgangskode guide" }
    ],
    "generationMs": 1240
  },
  "messages": [],
  "internalNotes": [],
  "lockedUntil": "2026-06-19T10:40:00Z"
}
```

**Response `423 Locked`:**
```json
{
  "lockedByAgent": "Søren Sørensen",
  "lockedUntil": "2026-06-19T10:35:00Z"
}
```

---

### POST /api/agent/tickets/{ticketId}/reply
Send svar til kunde.

**Request:**
```json
{
  "body": "Hej Jens, prøv at klikke på 'Glemt adgangskode'...",
  "aiFeedback": "RedigeretOgSendt",
  "overrideCategory": "Konto",
  "overridePriority": "Normal"
}
```

`aiFeedback` skal være én af: `Accepteret`, `RedigeretOgSendt`, `Afvist`

**Response `200 OK`** — status skifter til `AfventerKunde`.

---

### POST /api/agent/tickets/{ticketId}/keepalive
Forny lås med 10 minutter. Kaldes automatisk hvert 5. minut af klienten.

**Response `200 OK`**  
**Response `423 Locked`:** En anden agent har overtaget låsen.

---

### POST /api/agent/tickets/{ticketId}/assign
Tildel billet til agent eller team.

**Request:**
```json
{
  "agentId": "7bc91a23-4812-4321-a9fd-1b2c3d4e5f67",
  "note": "Sagen kræver adgang til backend-logs"
}
```

**Response `200 OK`**

---

### POST /api/agent/tickets/{ticketId}/close
Luk billet.

**Request:**
```json
{
  "reason": "Løst",
  "finalNote": "Kunden fik nulstillet sin adgangskode"
}
```

Gyldige `reason`-værdier: `Løst`, `Duplikat`, `KundeIkkeNåBare`, `UdenForScope`

**Response `200 OK`**  
**Response `422 Unprocessable Entity`:** Ugyldig statusovergang.

---

### POST /api/agent/tickets/{ticketId}/notes
Tilføj intern note (vises ikke for kunden).

**Request:**
```json
{
  "body": "Har kontaktet IT-afdelingen for yderligere info"
}
```

**Response `201 Created`**

---

## Adminendpoints

### GET /api/admin/knowledge-articles
Hent alle videnbaseartikler (inkl. inaktive).

**Response `200 OK`:** Liste af artikler med alle felter.

---

### POST /api/admin/knowledge-articles
Opret ny videnbaseartikel. Trigger re-embedding automatisk.

**Request:**
```json
{
  "title": "Nulstil adgangskode guide",
  "content": "## Sådan nulstiller du din adgangskode\n\n1. Gå til login-siden...",
  "tags": ["Konto", "Adgangskode"],
  "isActive": true
}
```

**Response `201 Created`**

---

### PUT /api/admin/knowledge-articles/{id}
Opdater artikel. Trigger re-embedding automatisk.

---

### DELETE /api/admin/knowledge-articles/{id}
Slet artikel og fjern embedding fra vector DB.

**Response `204 No Content`**

---

### GET /api/admin/categories
Hent alle kategorier.

---

### POST /api/admin/categories
Opret kategori.

---

### PUT /api/admin/categories/{id}
Opdater kategori. Kategorier med aktive billetter kan ikke omdøbes til eksisterende kategorinavn.

---

### GET /api/admin/reports/overview
Hent aggregerede rapportdata.

**Query params:** `from` og `to` (ISO 8601, f.eks. `2026-06-01`)

**Response `200 OK`:**
```json
{
  "period": { "from": "2026-06-01", "to": "2026-06-19" },
  "tickets": {
    "total": 142,
    "byStatus": {
      "Lukket": 98,
      "ÅbenTildelt": 22,
      "AfventerKunde": 18,
      "ÅbenUtildelt": 4
    },
    "byCategory": {
      "Konto": 61,
      "TekniskFejl": 38,
      "Fakturering": 27,
      "Generelt": 16
    },
    "byPriority": {
      "Kritisk": 8,
      "Høj": 34,
      "Normal": 87,
      "Lav": 13
    }
  },
  "ai": {
    "acceptRate": 0.64,
    "editRate": 0.21,
    "rejectRate": 0.15,
    "avgConfidence": 0.78,
    "avgGenerationMs": 1380
  },
  "sla": {
    "kritisk": { "withinSla": 6, "breach": 2, "rate": 0.75 },
    "høj": { "withinSla": 29, "breach": 5, "rate": 0.85 },
    "normal": { "withinSla": 72, "breach": 15, "rate": 0.83 },
    "lav": { "withinSla": 12, "breach": 1, "rate": 0.92 }
  },
  "agents": [
    {
      "agentId": "...",
      "name": "Søren Sørensen",
      "ticketsHandled": 38,
      "avgResolutionMinutes": 84
    }
  ]
}
```
