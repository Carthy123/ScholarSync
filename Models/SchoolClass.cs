using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class SchoolClass
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string AcademicYear { get; set; } = string.Empty;

    public ICollection<Student> Students { get; set; } = new List<Student>();
}