using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ScholarSync.Pages.Calendar;

public class IndexModel : PageModel
{
    private static readonly string[] EventTypes = { "Exam", "Excursion", "Midterm", "Vacation", "Holiday", "Other" };
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public IndexModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTime.Today.Year;

    [BindProperty(SupportsGet = true)]
    public int Month { get; set; } = DateTime.Today.Month;

    [BindProperty]
    public EventInput Input { get; set; } = new();

    public IList<SchoolEvent> Events { get; private set; } = new List<SchoolEvent>();
    public IReadOnlyList<DateTime> CalendarDays { get; private set; } = Array.Empty<DateTime>();
    public IReadOnlyList<string> AvailableEventTypes => EventTypes;
    public string MonthLabel => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

    public async Task OnGetAsync()
    {
        NormalizeMonth();
        await LoadCalendarAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!User.IsInRole("Admin"))
            return Forbid();

        if (!EventTypes.Contains(Input.EventType, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError("Input.EventType", "Select a valid event type.");
        if (Input.EndDate < Input.StartDate)
            ModelState.AddModelError("Input.EndDate", "The end date cannot be before the start date.");

        if (!ModelState.IsValid)
        {
            NormalizeMonth(Input.StartDate.Year, Input.StartDate.Month);
            await LoadCalendarAsync();
            return Page();
        }

        var schoolEvent = new SchoolEvent
        {
            Title = Input.Title.Trim(),
            EventType = Input.EventType,
            StartDate = Input.StartDate.Date,
            EndDate = Input.EndDate.Date,
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            Location = string.IsNullOrWhiteSpace(Input.Location) ? null : Input.Location.Trim(),
            CreatedBy = User.Identity?.Name
        };
        _context.SchoolEvents.Add(schoolEvent);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "SchoolEvent", schoolEvent.Id, $"Added {schoolEvent.EventType.ToLowerInvariant()} '{schoolEvent.Title}'.");
        return RedirectToPage(new { Year = schoolEvent.StartDate.Year, Month = schoolEvent.StartDate.Month });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!User.IsInRole("Admin"))
            return Forbid();

        var schoolEvent = await _context.SchoolEvents.FindAsync(id);
        if (schoolEvent != null)
        {
            _context.SchoolEvents.Remove(schoolEvent);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Deleted", "SchoolEvent", id, $"Deleted school event '{schoolEvent.Title}'.");
        }
        return RedirectToPage(new { Year, Month });
    }

    public async Task<FileContentResult> OnGetExportCsvAsync()
    {
        NormalizeMonth();
        var firstDay = new DateTime(Year, Month, 1);
        var nextMonth = firstDay.AddMonths(1);
        var events = await _context.SchoolEvents
            .AsNoTracking()
            .Where(item => item.StartDate < nextMonth && item.EndDate >= firstDay)
            .OrderBy(item => item.StartDate)
            .ThenBy(item => item.Title)
            .ToListAsync();
        var csv = new StringBuilder("Activity ID,Title,Type,Start Date,End Date,Location,Description,Created By\n");
        foreach (var schoolEvent in events)
            csv.AppendLine(string.Join(",", CsvValue(schoolEvent.Id.ToString()), CsvValue(schoolEvent.Title), CsvValue(schoolEvent.EventType), CsvValue(schoolEvent.StartDate.ToString("yyyy-MM-dd")), CsvValue(schoolEvent.EndDate.ToString("yyyy-MM-dd")), CsvValue(schoolEvent.Location), CsvValue(schoolEvent.Description), CsvValue(schoolEvent.CreatedBy)));
        await _auditService.LogAsync("Exported", "SchoolEvent", details: $"Exported {events.Count} calendar activity record(s) for {firstDay:MMMM yyyy}.");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"school-calendar-{Year}-{Month:00}.csv");
    }

    private static string CsvValue(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

    private async Task LoadCalendarAsync()
    {
        var firstDay = new DateTime(Year, Month, 1);
        var calendarStart = firstDay.AddDays(-(int)firstDay.DayOfWeek);
        CalendarDays = Enumerable.Range(0, 42).Select(offset => calendarStart.AddDays(offset)).ToArray();
        var calendarEnd = calendarStart.AddDays(42);
        Events = await _context.SchoolEvents
            .AsNoTracking()
            .Where(item => item.StartDate < calendarEnd && item.EndDate >= calendarStart)
            .OrderBy(item => item.StartDate)
            .ThenBy(item => item.Title)
            .ToListAsync();
    }

    private void NormalizeMonth(int? year = null, int? month = null)
    {
        Year = year is >= 2000 and <= 2100 ? year.Value : Year;
        Month = month is >= 1 and <= 12 ? month.Value : Month;
        if (Year is < 2000 or > 2100)
            Year = DateTime.Today.Year;
        if (Month is < 1 or > 12)
            Month = DateTime.Today.Month;
    }

    public class EventInput
    {
        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string EventType { get; set; } = "Exam";
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today;
        [StringLength(250)]
        public string? Description { get; set; }
        [StringLength(150)]
        public string? Location { get; set; }
    }
}
