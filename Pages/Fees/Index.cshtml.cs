using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Fees;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;
    private readonly SchoolAccessService _accessService;
    private readonly AcademicYearService _academicYearService;

    public IndexModel(SchoolDbContext context, AuditService auditService, SchoolAccessService accessService, AcademicYearService academicYearService)
    {
        _context = context;
        _auditService = auditService;
        _accessService = accessService;
        _academicYearService = academicYearService;
    }

    [BindProperty]
    public FeeInput Input { get; set; } = new();

    public IList<Student> Students { get; private set; } = new List<Student>();
    public IList<FeePayment> Payments { get; private set; } = new List<FeePayment>();
    public IList<BalanceRow> Balances { get; private set; } = new List<BalanceRow>();
    public decimal TotalExpected => Balances.Sum(row => row.Expected);
    public decimal TotalOutstanding => Balances.Sum(row => row.Outstanding);
    [BindProperty(SupportsGet = true)] public string? BalanceAcademicYear { get; set; }
    [BindProperty(SupportsGet = true)] public string? BalanceTerm { get; set; }
    public decimal TotalReceived => Payments.Sum(payment => payment.Amount);
    public IReadOnlyList<string> Terms { get; } = new[] { "First Term", "Second Term", "Third Term" };

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.AcademicYear))
            Input.AcademicYear = await _academicYearService.GetCurrentAsync();

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }
        if (!await _accessService.CanAccessStudentAsync(Input.StudentId))
            return Forbid();

        var payment = new FeePayment
        {
            StudentId = Input.StudentId,
            Term = Input.Term,
            AcademicYear = Input.AcademicYear.Trim(),
            Amount = Input.Amount,
            ReceiptNumber = string.IsNullOrWhiteSpace(Input.ReceiptNumber) ? null : Input.ReceiptNumber.Trim(),
            PaymentDate = Input.PaymentDate.Date,
            Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim()
        };
        _context.FeePayments.Add(payment);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "FeePayment", payment.Id, $"Recorded GHS {payment.Amount:N2} fee payment.");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Students = await _context.Students.Include(student => student.SchoolClass).AsNoTracking().OrderBy(student => student.LastName).ThenBy(student => student.FirstName).ToListAsync();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            Students = Students.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)).ToList();
        }
        Payments = await _context.FeePayments.Include(payment => payment.Student).AsNoTracking().OrderByDescending(payment => payment.PaymentDate).ThenByDescending(payment => payment.Id).Take(200).ToListAsync();
        if (string.IsNullOrWhiteSpace(Input.AcademicYear))
            Input.AcademicYear = await _academicYearService.GetCurrentAsync();
        if (Input.PaymentDate == default)
            Input.PaymentDate = DateTime.Today;

        BalanceAcademicYear ??= Input.AcademicYear;
        BalanceTerm ??= Terms[0];
        var classIds = Students.Where(student => student.SchoolClassId.HasValue).Select(student => student.SchoolClassId!.Value).Distinct().ToList();
        var structures = await _context.FeeStructures.Where(item => classIds.Contains(item.SchoolClassId) && item.AcademicYear == BalanceAcademicYear && item.Term == BalanceTerm).AsNoTracking().ToListAsync();
        var studentIds = Students.Select(student => student.Id).ToList();
        var payments = await _context.FeePayments.Where(payment => studentIds.Contains(payment.StudentId) && payment.AcademicYear == BalanceAcademicYear && payment.Term == BalanceTerm).AsNoTracking().ToListAsync();
        Balances = Students.Select(student =>
        {
            var expected = student.SchoolClassId.HasValue ? structures.FirstOrDefault(item => item.SchoolClassId == student.SchoolClassId.Value)?.Amount ?? 0 : 0;
            var paid = payments.Where(payment => payment.StudentId == student.Id).Sum(payment => payment.Amount);
            return new BalanceRow { Student = student, Expected = expected, Paid = paid, Outstanding = Math.Max(expected - paid, 0) };
        }).ToList();
    }

    public class FeeInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a student.")] public int StudentId { get; set; }
        [Required] public string Term { get; set; } = "First Term";
        [Required, StringLength(30)] public string AcademicYear { get; set; } = string.Empty;
        [Range(0.01, 100000000, ErrorMessage = "Enter an amount greater than zero.")] public decimal Amount { get; set; }
        [StringLength(100)] public string? ReceiptNumber { get; set; }
        [DataType(DataType.Date)] public DateTime PaymentDate { get; set; } = DateTime.Today;
        [StringLength(250)] public string? Notes { get; set; }
    }

    public class BalanceRow
    {
        public Student Student { get; set; } = null!;
        public decimal Expected { get; set; }
        public decimal Paid { get; set; }
        public decimal Outstanding { get; set; }
    }
}
