using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class GradeRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    [Required, StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal Score { get; set; }

    public string Grade => Score >= 80 ? "A" : Score >= 70 ? "B" : Score >= 60 ? "C" : Score >= 50 ? "D" : "F";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
