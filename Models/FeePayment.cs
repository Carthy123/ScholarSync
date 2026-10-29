using System.ComponentModel.DataAnnotations;

namespace ScholarSync.Models;

public class FeePayment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [Required, StringLength(30)] public string Term { get; set; } = "First Term";
    [Required, StringLength(30)] public string AcademicYear { get; set; } = string.Empty;
    [Range(0.01, 100000000)] public decimal Amount { get; set; }
    [StringLength(100)] public string? ReceiptNumber { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    [StringLength(250)] public string? Notes { get; set; }
}
