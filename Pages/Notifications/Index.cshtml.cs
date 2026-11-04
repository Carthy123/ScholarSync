using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Security;
using System.Globalization;

namespace ScholarSync.Pages.Notifications;

public class IndexModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public IndexModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    public IList<NotificationItem> Notifications { get; private set; } = new List<NotificationItem>();
    public int OpenCount => Notifications.Count;

    public async Task OnGetAsync()
    {
        var settings = await _context.SchoolSettings.AsNoTracking().FirstOrDefaultAsync(item => item.Id == 1);
        var academicYear = settings?.AcademicYear ?? DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        var visibleStudents = await _context.Students.AsNoTracking()
            .Where(student => _accessService.IsAdmin || (student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)))
            .Select(student => new { student.Id, student.SchoolClassId }).ToListAsync();
        var visibleStudentIds = visibleStudents.Select(student => student.Id).ToList();
        var today = DateTime.Today;

        if (_accessService.IsAdmin)
        {
            var unassigned = await _context.Students.CountAsync(student => student.SchoolClassId == null);
            if (unassigned > 0) Add("Students need class assignment", $"{unassigned} student(s) are not assigned to a class.", "Action required", "warning", "/Students/Index");
        }

        var markedToday = await _context.AttendanceRecords.CountAsync(record => visibleStudentIds.Contains(record.StudentId) && record.AttendanceDate == today);
        if (visibleStudentIds.Count > 0 && markedToday < visibleStudentIds.Count)
            Add("Attendance is incomplete", $"{visibleStudentIds.Count - markedToday} student(s) still need attendance for today.", "Today", "info", "/Attendance/Index");

        var upcoming = await _context.SchoolEvents.AsNoTracking().Where(item => item.EndDate >= today && item.StartDate <= today.AddDays(30)).OrderBy(item => item.StartDate).Take(8).ToListAsync();
        foreach (var item in upcoming)
            Add(item.Title, $"{item.EventType} · {item.StartDate:dd MMM yyyy}{(item.StartDate == item.EndDate ? string.Empty : $" to {item.EndDate:dd MMM yyyy}")}", "Upcoming", "calendar", $"/Calendar/Index?year={item.StartDate.Year}&month={item.StartDate.Month}");

        var locked = _accessService.IsAdmin ? await _context.AdminUsers.CountAsync(user => user.LockoutEnd > DateTime.UtcNow) : 0;
        if (locked > 0) Add("Account security needs attention", $"{locked} account(s) are temporarily locked.", "Security", "danger", "/Account/Audit");

        var feeStructures = await _context.FeeStructures.AsNoTracking().Where(item => item.AcademicYear == academicYear && visibleStudents.Select(student => student.SchoolClassId).Contains(item.SchoolClassId)).ToListAsync();
        var paymentRows = await _context.FeePayments.AsNoTracking().Where(item => item.AcademicYear == academicYear && visibleStudentIds.Contains(item.StudentId)).Select(item => new { item.StudentId, item.Amount }).ToListAsync();
        var paidByStudent = paymentRows.GroupBy(item => item.StudentId).ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));
        var studentsWithBalances = visibleStudents.Where(student => student.SchoolClassId.HasValue).Count(student => feeStructures.Where(item => item.SchoolClassId == student.SchoolClassId!.Value).Sum(item => item.Amount) > paidByStudent.GetValueOrDefault(student.Id));
        if (studentsWithBalances > 0) Add("Fee balances require review", $"{studentsWithBalances} student(s) have outstanding balances for {academicYear}.", "Finance", "money", "/Fees/Index");
    }

    private void Add(string title, string message, string label, string style, string link) => Notifications.Add(new NotificationItem(title, message, label, style, link));
    public sealed record NotificationItem(string Title, string Message, string Label, string Style, string Link);
}
