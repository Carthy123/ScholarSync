using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class SchoolSubject
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Level { get; set; } = "All levels";

    public bool IsActive { get; set; } = true;
}
