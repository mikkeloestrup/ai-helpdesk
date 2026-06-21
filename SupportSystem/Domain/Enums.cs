namespace SupportSystem.Domain;

public enum TicketStatus
{
    Ny,
    UnderAiAnalyse,
    ÅbenUtildelt,
    ÅbenTildelt,
    AfventerKunde,
    Eskaleret,
    Lukket
}

public enum Priority
{
    Lav,
    Normal,
    Høj,
    Kritisk
}

public enum Sentiment
{
    Positiv,
    Neutral,
    Negativ,
    TydeligFrustreret
}

/// <summary>AI'ens faste klassifikations-skema — adskilt fra den admin-styrede Category-tabel.</summary>
public enum AiCategory
{
    Fakturering,
    TekniskFejl,
    Konto,
    Generelt,
    Ukendt
}

public enum Direction
{
    Indgående,
    Udgående
}

public enum ClosureReason
{
    Løst,
    Duplikat,
    KundeIkkeNåBare,
    UdenForScope,
    LuketAfKunde
}

public enum AiFeedback
{
    Accepteret,
    RedigeretOgSendt,
    Afvist
}
