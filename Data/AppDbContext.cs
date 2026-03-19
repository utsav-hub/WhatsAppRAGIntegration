using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using WhatsAppDev.Models;

namespace WhatsAppDev.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<ChatbotKeyword> ChatbotKeywords => Set<ChatbotKeyword>();
    public DbSet<ChatbotSetting> ChatbotSettings => Set<ChatbotSetting>();
    public DbSet<ChatbotFAQ> ChatbotFAQs => Set<ChatbotFAQ>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<KnowledgeIngestionJob> KnowledgeIngestionJobs => Set<KnowledgeIngestionJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasMany(u => u.Conversations)
            .WithOne(c => c.User!)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatbotSetting>()
            .HasIndex(s => s.SettingKey)
            .IsUnique();

        modelBuilder.Entity<KnowledgeChunk>()
            .Property(x => x.Embedding)
            .HasColumnType("vector(768)");
    }
}
