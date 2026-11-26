using Document.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Document.Api.Data;

public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
{
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<DocumentHistoryEntity> DocumentHistories => Set<DocumentHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(d => d.Version).IsConcurrencyToken();
            entity.HasMany(d => d.History)
                .WithOne(h => h.Document)
                .HasForeignKey(h => h.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentHistoryEntity>(entity =>
        {
            entity.ToTable("DocumentHistories");
            entity.HasKey(h => h.HistoryId);
            entity.Property(h => h.FromStatus).HasConversion<string?>().HasMaxLength(30);
            entity.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(h => h.DocumentId);
            entity.HasIndex(h => new { h.DocumentId, h.IdempotencyKey })
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
        });
    }
}
