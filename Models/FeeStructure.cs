using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class FeeStructure
{
    public int Id { get; set; }
    public int SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = null!;
    [Required, StringLength(30)] public string Term { get; set; } = "First Term";
    [Required, StringLength(30)] public string AcademicYear { get; set; } = string.Empty;
    [Range(0.01, 100000000)] public decimal Amount { get; set; }
    [StringLength(150)] public string? Description { get; set; }
}
