using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace WhatsAppDev.Models;

public class KnowledgeChunk
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public Guid DocumentId { get; set; }

    public KnowledgeDocument? Document { get; set; }

    [Required]
    public string ChunkText { get; set; } = string.Empty;

    /// <summary>
    /// Embedding for this chunk (vector(768) in PostgreSQL).
    /// </summary>
    public Vector? Embedding { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

