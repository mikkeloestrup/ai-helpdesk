using Microsoft.EntityFrameworkCore;
using SupportSystem.Domain;

namespace SupportSystem.Data;

public class SupportDbContext(DbContextOptions<SupportDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<AiAnalysis> AiAnalyses => Set<AiAnalysis>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Ticket>(e =>
        {
            e.Property(t => t.TicketNumber).HasMaxLength(32);
            e.HasIndex(t => t.TicketNumber).IsUnique();
            e.Property(t => t.Subject).HasMaxLength(200);
            e.Property(t => t.Description).HasMaxLength(5000);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(t => t.Priority).HasConversion<string>().HasMaxLength(16);
            e.Property(t => t.ClosureReason).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(t => new { t.Status, t.Priority });

            e.HasOne(t => t.Category).WithMany().HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.AssignedToAgent).WithMany().HasForeignKey(t => t.AssignedToAgentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.AiAnalysis).WithOne(a => a.Ticket).HasForeignKey<AiAnalysis>(a => a.TicketId);
            e.HasMany(t => t.Messages).WithOne().HasForeignKey(m => m.TicketId);
            e.HasMany(t => t.Notes).WithOne().HasForeignKey(n => n.TicketId);
        });

        b.Entity<AiAnalysis>(e =>
        {
            e.Property(a => a.SuggestedCategory).HasConversion<string>().HasMaxLength(32);
            e.Property(a => a.Sentiment).HasConversion<string>().HasMaxLength(32);
            e.Property(a => a.CategoryConfidence).HasPrecision(4, 3);
            e.Property(a => a.Feedback).HasConversion<string>().HasMaxLength(32);

            // Ægte many-to-many med implicit join-tabel
            e.HasMany(a => a.SourceArticles).WithMany(k => k.UsedInAnalyses)
                .UsingEntity(j => j.ToTable("AiAnalysisSourceArticles"));
        });

        b.Entity<KnowledgeArticle>(e =>
        {
            e.Property(k => k.Title).HasMaxLength(200);
            // Embedding er en SQL Server 2025 VECTOR(1024)-kolonne (bge-m3). EF Core 10 mapper
            // float[] som JSON primitive collection, ikke som vector, så kolonnen oprettes via
            // SQL i migrationen og holdes uden for EF-modellen. Vektor-IO sker SQL-side (issue #13).
            e.Ignore(k => k.Embedding);
        });

        b.Entity<Message>(e =>
            e.Property(m => m.Direction).HasConversion<string>().HasMaxLength(16));

        b.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(64);
            e.HasIndex(c => c.Name).IsUnique();
        });

        b.Entity<Agent>(e =>
        {
            e.Property(a => a.Name).HasMaxLength(128);
            e.Property(a => a.Email).HasMaxLength(256);
            e.HasIndex(a => a.Email).IsUnique();
        });
    }
}
