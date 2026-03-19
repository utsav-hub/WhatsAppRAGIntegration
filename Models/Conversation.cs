using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppDev.Models;

public class Conversation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public User? User { get; set; }

    [Required]
    public string UserMessage { get; set; } = string.Empty;

    public string? BotResponse { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

