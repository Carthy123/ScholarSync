using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class SchoolEvent
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string EventType { get; set; } = "Other";

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [StringLength(250)]
    public string? Description { get; set; }

    [StringLength(150)]
    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
