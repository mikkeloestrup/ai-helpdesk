namespace SupportSystem.Domain;

public class AiAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = default!;

    /// <summary>AI'ens rå kategori-forslag (mappes til Ticket.CategoryId i pipelinen).</summary>
    public AiCategory SuggestedCategory { get; set; }
    public decimal CategoryConfidence { get; set; }
    public Sentiment Sentiment { get; set; }
    public string SuggestedReply { get; set; } = default!;

    /// <summary>Artikler brugt i RAG — ægte many-to-many (implicit join-tabel).</summary>
    public List<KnowledgeArticle> SourceArticles { get; } = [];

    public int GenerationMs { get; set; }

    public AiFeedback? Feedback { get; set; }
    public int? FeedbackCharDiff { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
