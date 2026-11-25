using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class CommunicationTemplate
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Category { get; set; } = "General";
    [Required, StringLength(20)] public string Channel { get; set; } = "In-app";
    [Required, StringLength(150)] public string Subject { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
