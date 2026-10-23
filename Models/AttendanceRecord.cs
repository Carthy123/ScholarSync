namespace ScholarSync.Models;

public class AttendanceRecord
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime AttendanceDate { get; set; }
    public string Status { get; set; } = "Present";
}
