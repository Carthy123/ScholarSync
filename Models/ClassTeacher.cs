namespace ScholarSync.Models;

public class ClassTeacher
{
    public int Id { get; set; }
    public int SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = null!;
    public int AdminUserId { get; set; }
    public AdminUser AdminUser { get; set; } = null!;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
