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

    [StringLength(20)] public string GuardianPreferredChannel { get; set; } = "Any";
    public bool GuardianCommunicationOptOut { get; set; }
    public bool GuardianContactVerified { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    [StringLength(300)]
    public string? PhotoPath { get; set; }

    [StringLength(500)] public string? MedicalConditions { get; set; }
    [StringLength(500)] public string? Allergies { get; set; }
    [StringLength(30)] public string? BloodGroup { get; set; }
    [StringLength(500)] public string? MedicationNotes { get; set; }
    [StringLength(150)] public string? DoctorName { get; set; }
    [StringLength(50)] public string? DoctorPhone { get; set; }
    [StringLength(1000)] public string? WelfareNotes { get; set; }

    public int? SchoolClassId { get; set; }

    public SchoolClass? SchoolClass { get; set; }

    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedBy { get; set; }
}
