using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class GuardianCommunication
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [Required, StringLength(150)] public string RecipientName { get; set; } = string.Empty;
    [StringLength(150)] public string? RecipientEmail { get; set; }
    [StringLength(40)] public string? RecipientPhone { get; set; }
    [Required, StringLength(20)] public string Channel { get; set; } = "In-app";
    [Required, StringLength(150)] public string Subject { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Message { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Status { get; set; } = "Recorded";
    [StringLength(300)] public string? StatusDetails { get; set; }
    [StringLength(100)] public string? ProviderMessageId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string? CreatedBy { get; set; }
}
