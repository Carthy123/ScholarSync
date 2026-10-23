using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class SchoolSettings
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string SchoolName { get; set; } = "ScholarSync School";
    [Required, StringLength(100)] public string AcademicYear { get; set; } = "2026";
    [Required, StringLength(200)] public string WelcomeMessage { get; set; } = "Welcome back. Here’s what’s happening today.";
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use a 6-digit hex color such as #159D98.")] public string PrimaryColor { get; set; } = "#142B4A";
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use a 6-digit hex color such as #40D6C2.")] public string AccentColor { get; set; } = "#40D6C2";
}
