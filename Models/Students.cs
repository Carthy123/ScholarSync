using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class Student
{
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? GuardianName { get; set; }

    public string? GuardianPhone { get; set; }

    public string? GuardianEmail { get; set; }

    public string? GuardianAddress { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public int? SchoolClassId { get; set; }

    public SchoolClass? SchoolClass { get; set; }
}
