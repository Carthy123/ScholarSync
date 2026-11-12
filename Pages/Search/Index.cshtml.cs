using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Security;

namespace ScholarSync.Pages.Search;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public IndexModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public IList<SearchResult> Results { get; private set; } = new List<SearchResult>();
    public int ResultCount => Results.Count;

    public async Task OnGetAsync()
    {
        Query = Query?.Trim();
        if (string.IsNullOrWhiteSpace(Query) || Query.Length < 2)
            return;

        var search = Query;
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        var visibleStudents = _context.Students
            .Include(student => student.SchoolClass)
            .AsNoTracking()
            .Where(student => _accessService.IsAdmin || (student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)));

        var students = await visibleStudents
            .Where(student => student.FirstName.Contains(search) || student.LastName.Contains(search) || (student.GuardianName != null && student.GuardianName.Contains(search)) || (student.GuardianPhone != null && student.GuardianPhone.Contains(search)))
            .OrderBy(student => student.LastName).ThenBy(student => student.FirstName).Take(10).ToListAsync();
        foreach (var student in students)
            Add("Student", $"{student.FirstName} {student.LastName}", $"{student.SchoolClass?.Name ?? "Unassigned"} · Guardian: {student.GuardianName ?? "Not provided"}", $"/Students/Details/{student.Id}");

        var guardians = await visibleStudents
            .Where(student => student.GuardianName != null && (student.GuardianName.Contains(search) || (student.GuardianPhone != null && student.GuardianPhone.Contains(search)) || (student.GuardianEmail != null && student.GuardianEmail.Contains(search))))
            .Select(student => student.GuardianName!).Distinct().Take(10).ToListAsync();
        foreach (var guardian in guardians)
            Add("Guardian", guardian, "Parent/Guardian Directory", $"/Guardians/Index?SearchTerm={Uri.EscapeDataString(guardian)}");

        var classes = await _context.Classes.AsNoTracking().Where(schoolClass => schoolClass.Name.Contains(search) || schoolClass.AcademicYear.Contains(search)).OrderBy(schoolClass => schoolClass.Name).Take(10).ToListAsync();
        foreach (var schoolClass in classes)
            Add("Class", schoolClass.Name, $"Academic year {schoolClass.AcademicYear}", "/Classes/Index");

        var gradesQuery = _context.GradeRecords.Include(record => record.Student).AsNoTracking().Where(record => record.Subject.Contains(search) || record.Term.Contains(search) || record.AcademicYear.Contains(search) || record.Student.FirstName.Contains(search) || record.Student.LastName.Contains(search));
        if (!_accessService.IsAdmin) gradesQuery = gradesQuery.Where(record => record.Student.SchoolClassId.HasValue && allowedClassIds.Contains(record.Student.SchoolClassId.Value));
        var grades = await gradesQuery.OrderByDescending(record => record.RecordedAt).Take(10).ToListAsync();
        foreach (var grade in grades)
            Add("Grade", $"{grade.Student.FirstName} {grade.Student.LastName} · {grade.Subject}", $"{grade.Score:N0}% · {grade.Term}, {grade.AcademicYear}", "/Grades/Index");

        var paymentsQuery = _context.FeePayments.Include(payment => payment.Student).AsNoTracking().Where(payment => (payment.ReceiptNumber != null && payment.ReceiptNumber.Contains(search)) || payment.Term.Contains(search) || payment.AcademicYear.Contains(search) || payment.Student.FirstName.Contains(search) || payment.Student.LastName.Contains(search));
        if (!_accessService.IsAdmin) paymentsQuery = paymentsQuery.Where(payment => payment.Student.SchoolClassId.HasValue && allowedClassIds.Contains(payment.Student.SchoolClassId.Value));
        var payments = await paymentsQuery.OrderByDescending(payment => payment.PaymentDate).Take(10).ToListAsync();
        foreach (var payment in payments)
            Add("Fee payment", $"{payment.Student.FirstName} {payment.Student.LastName}", $"GHS {payment.Amount:N2} · {payment.Term} · Receipt {payment.ReceiptNumber ?? "—"}", "/Fees/Index");

        var events = await _context.SchoolEvents.AsNoTracking().Where(item => item.Title.Contains(search) || item.EventType.Contains(search) || (item.Description != null && item.Description.Contains(search))).OrderBy(item => item.StartDate).Take(10).ToListAsync();
        foreach (var schoolEvent in events)
            Add("Calendar", schoolEvent.Title, $"{schoolEvent.EventType} · {schoolEvent.StartDate:dd MMM yyyy}", $"/Calendar/Index?year={schoolEvent.StartDate.Year}&month={schoolEvent.StartDate.Month}");

        var announcements = await _context.SchoolAnnouncements.AsNoTracking().Where(item => (item.Audience == "Everyone" || (item.Audience == "Teachers" && User.IsInRole("Teacher")) || (item.Audience == "Administrators" && User.IsInRole("Admin"))) && (item.Title.Contains(search) || item.Message.Contains(search) || item.Category.Contains(search))).OrderByDescending(item => item.CreatedAt).Take(10).ToListAsync();
        foreach (var announcement in announcements)
            Add("Bulletin", announcement.Title, $"{announcement.Category} · {announcement.Priority}", "/Announcements/Index");
    }

    private void Add(string category, string title, string summary, string link) => Results.Add(new SearchResult(category, title, summary, link));
    public sealed record SearchResult(string Category, string Title, string Summary, string Link);
}
