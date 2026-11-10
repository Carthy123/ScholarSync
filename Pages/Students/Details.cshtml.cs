using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Students;

public class DetailsModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public DetailsModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    public Student Student { get; private set; } = null!;
    public IList<GradeRecord> Grades { get; private set; } = new List<GradeRecord>();
    public IList<AttendanceRecord> Attendance { get; private set; } = new List<AttendanceRecord>();
    public IList<StudentEnrollment> EnrollmentHistory { get; private set; } = new List<StudentEnrollment>();
    public IList<FeePayment> Payments { get; private set; } = new List<FeePayment>();
    public IList<AuditLog> Activity { get; private set; } = new List<AuditLog>();
    public decimal TotalPaid => Payments.Sum(payment => payment.Amount);
    public decimal ExpectedFees { get; private set; }
    public decimal OutstandingFees => Math.Max(ExpectedFees - TotalPaid, 0);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var student = await _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking()
            .FirstOrDefaultAsync(student => student.Id == id);

        if (student == null)
        {
            return NotFound();
        }
        if (!await _accessService.CanAccessStudentAsync(id))
            return Forbid();

        Student = student;

        Grades = await _context.GradeRecords
            .Where(record => record.StudentId == id)
            .AsNoTracking()
            .OrderByDescending(record => record.RecordedAt)
            .ToListAsync();

        Attendance = await _context.AttendanceRecords
            .Where(record => record.StudentId == id)
            .AsNoTracking()
            .OrderByDescending(record => record.AttendanceDate)
            .Take(10)
            .ToListAsync();

        EnrollmentHistory = await _context.StudentEnrollments
            .Where(enrollment => enrollment.StudentId == id)
            .Include(enrollment => enrollment.SchoolClass)
            .AsNoTracking()
            .OrderByDescending(enrollment => enrollment.AcademicYear)
            .ToListAsync();

        Payments = await _context.FeePayments
            .Where(payment => payment.StudentId == id)
            .AsNoTracking()
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.Id)
            .Take(20)
            .ToListAsync();

        var schoolYear = await _context.SchoolSettings
            .Where(settings => settings.Id == 1)
            .Select(settings => settings.AcademicYear)
            .FirstOrDefaultAsync() ?? DateTime.UtcNow.Year.ToString();
        if (student.SchoolClassId.HasValue)
        {
            var feeStructures = await _context.FeeStructures
                .Where(structure => structure.SchoolClassId == student.SchoolClassId.Value && structure.AcademicYear == schoolYear)
                .AsNoTracking()
                .ToListAsync();
            ExpectedFees = feeStructures.Sum(structure => structure.Amount);
        }

        Activity = await _context.AuditLogs
            .Where(log => log.EntityType == "Student" && log.EntityId == id)
            .AsNoTracking()
            .OrderByDescending(log => log.CreatedAt)
            .Take(20)
            .ToListAsync();

        return Page();
    }
}
