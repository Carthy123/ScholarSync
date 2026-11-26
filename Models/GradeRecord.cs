using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class GradeRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    [Required, StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Term { get; set; } = "First Term";

    [Required, StringLength(30)]
    public string AcademicYear { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal Score { get; set; }

    public string Grade => GradingScale.GetGrade(Score, new SchoolSettings());
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
