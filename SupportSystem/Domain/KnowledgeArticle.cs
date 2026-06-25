namespace SupportSystem.Domain;

public class KnowledgeArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public List<string> Tags { get; set; } = [];

    /// <summary>bge-m3 embedding (1024 dim). SQL Server 2025 VECTOR-kolonne. Null indtil beregnet.</summary>
    public float[]? Embedding { get; set; }

    public bool IsActive { get; set; } = true;
    public int TimesUsedInRag { get; set; }

    public List<AiAnalysis> UsedInAnalyses { get; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
