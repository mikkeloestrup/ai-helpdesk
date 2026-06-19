# Forretningsregler

## Billetstatus-maskine

\`\`\`
Ny
 └─► UnderAiAnalyse        (AI-analysejob starter automatisk)
      ├─► ÅbenUtildelt      (AI færdig, ingen agent tildelt)
      │    ├─► ÅbenTildelt  (agent tager eller tildeles billetten)
      │    │    ├─► AfventerKunde   (agent har svaret)
      │    │    │    ├─► ÅbenTildelt     (kunde svarer tilbage)
      │    │    │    └─► Lukket          (ingen respons / løst)
      │    │    └─► Lukket          (agent lukker direkte)
      │    └─► Lukket        (admin lukker utildelt billet)
      └─► Eskaleret          (sentiment = TydeligFrustreret)
           └─► ÅbenTildelt  (eskalerede billetter tildeles straks)
\`\`\`

Ugyldige statusovergange returnerer `422 Unprocessable Entity`.

## Prioritetsregler

Prioritet beregnes automatisk efter AI-analyse. Reglerne evalueres i rækkefølge — første match vinder.

| Regel | Prioritet |
|---|---|
| AI-sentiment = `TydeligFrustreret` | **Kritisk** (overtrumfer alle andre regler) |
| AI-konfidensscore < 0.6 | **Høj** |
| Beskrivelse indeholder: "nedbrud", "kan ikke logge ind", "betaling fejler" | **Høj** |
| Standard (ingen af ovenstående) | **Normal** |
| Intern test-flag sat på billet | **Lav** |

Agenter kan altid overskrive prioriteten manuelt.

## SLA-definition

SLA måles i **arbejdstimer** (08:00–17:00, mandag–fredag, danske helligdage ekskluderet).

| Prioritet | Første respons | Løsningstid |
|---|---|---|
| Kritisk | 1 time | 4 timer |
| Høj | 4 timer | 24 timer |
| Normal | 8 timer | 72 timer |
| Lav | 24 timer | 7 dage |

`SlaDeadline` beregnes ud fra løsningstiden ved AI-analysens færdiggørelse.  
`SlaBreach = true` sættes automatisk af Hangfire-job der kører hvert 5. minut.

## Locking-regler

- Når agent åbner en billet sættes `LockedByAgentId` + `LockedUntil = now + 10 min`
- Anden agent der forsøger at åbne samme billet modtager `423 Locked` med navn og udløbstidspunkt
- Låsen fornyes automatisk hvert 5. minut via agent-ping (`POST /api/agent/tickets/{id}/keepalive`)
- Låsen frigives automatisk ved: afsendelse af svar, lukning, eller timeout

## Eskaleringsregler

Automatisk eskalering sker udelukkende baseret på AI-sentiment:

1. AI detekterer `TydeligFrustreret`
2. Prioritet sættes til `Kritisk`
3. Status sættes til `Eskaleret`
4. Der oprettes automatisk en intern note: _"AI-eskalering: sentiment detekteret som TydeligFrustreret"_
5. Billetten tildeles første ledige agent (eller placeres øverst i utildelt kø)

## Dataretention

| Data | Opbevaringsperiode |
|---|---|
| Åbne billetter | Ubegrænset |
| Lukkede billetter | 2 år fra lukningstidspunkt |
| AI-analysedata | Følger billetens levetid |
| Interne noter | Følger billetens levetid |

Billetter ældre end 90 dage returnerer `404` for kunder — men er stadig tilgængelige for agenter og admins.