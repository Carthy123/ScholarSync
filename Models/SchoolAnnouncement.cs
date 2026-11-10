using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class SchoolAnnouncement
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Message { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Category { get; set; } = "General";
    [Required, StringLength(20)] public string Priority { get; set; } = "Normal";
    [Required, StringLength(20)] public string Audience { get; set; } = "Everyone";
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string? CreatedBy { get; set; }
}
