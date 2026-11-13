using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Students;

public class ImportModel : PageModel
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;
    private readonly SchoolAccessService _accessService;
    private readonly IWebHostEnvironment _environment;

    public ImportModel(SchoolDbContext context, AuditService auditService, SchoolAccessService accessService, IWebHostEnvironment environment)
    {
        _context = context;
        _auditService = auditService;
        _accessService = accessService;
        _environment = environment;
    }

    [BindProperty]
    public IFormFile? ImportFile { get; set; }

    [BindProperty]
    public string? ImportToken { get; set; }

    public List<ImportRow> PreviewRows { get; private set; } = new();
    public string? UploadedFileName { get; private set; }
    public string? PreviewMessage { get; private set; }
    public bool HasPreview => !string.IsNullOrWhiteSpace(ImportToken) && PreviewRows.Count > 0;
    public bool PreviewIsValid => HasPreview && PreviewRows.All(row => row.IsValid);

    public IActionResult OnGet()
    {
        return _accessService.IsAdmin ? Page() : Forbid();
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        if (!_accessService.IsAdmin)
            return Forbid();

        if (ImportFile == null || ImportFile.Length == 0)
        {
            ModelState.AddModelError(nameof(ImportFile), "Choose a CSV file to upload.");
            return Page();
        }

        if (ImportFile.Length > MaxFileSize)
        {
            ModelState.AddModelError(nameof(ImportFile), "The CSV file must be 10 MB or smaller.");
            return Page();
        }

        if (!string.Equals(Path.GetExtension(ImportFile.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(ImportFile), "Upload a .csv file. The template can be opened and edited in Excel.");
            return Page();
        }

        var token = $"{Guid.NewGuid():N}.csv";
        var path = GetTempPath(token);
        await using (var stream = System.IO.File.Create(path))
        {
            await ImportFile.CopyToAsync(stream);
        }

        ImportToken = token;
        UploadedFileName = Path.GetFileName(ImportFile.FileName);
        PreviewRows = await ReadAndValidateAsync(path);
        PreviewMessage = PreviewRows.Count == 0
            ? "The file did not contain any student rows."
            : $"Review complete: {PreviewRows.Count(row => row.IsValid)} ready to import and {PreviewRows.Count(row => !row.IsValid)} with issues.";
        return Page();
    }

    public async Task<IActionResult> OnPostCommitAsync()
    {
        if (!_accessService.IsAdmin)
            return Forbid();

        if (string.IsNullOrWhiteSpace(ImportToken) || !IsSafeToken(ImportToken))
        {
            ModelState.AddModelError(string.Empty, "The import preview has expired. Upload the file again.");
            return Page();
        }

        var path = GetTempPath(ImportToken);
        if (!System.IO.File.Exists(path))
        {
            ModelState.AddModelError(string.Empty, "The import preview has expired. Upload the file again.");
            return Page();
        }

        PreviewRows = await ReadAndValidateAsync(path);
        UploadedFileName = "the selected CSV file";
        if (PreviewRows.Count == 0 || PreviewRows.Any(row => !row.IsValid))
        {
            PreviewMessage = "Import stopped because the file still contains issues. Correct them and upload the file again.";
            return Page();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var row in PreviewRows)
            {
                _context.Students.Add(row.ToStudent());
            }

            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Imported", "Student", details: $"Imported {PreviewRows.Count} student record(s) from a CSV file.");
            await transaction.CommitAsync();
            System.IO.File.Delete(path);
            TempData["SuccessMessage"] = $"Successfully imported {PreviewRows.Count} student record(s).";
            return RedirectToPage("/Students/Index");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public IActionResult OnGetTemplateCsv()
    {
        if (!_accessService.IsAdmin)
            return Forbid();

        var csv = "First Name,Last Name,Date of Birth,Gender,Guardian Name,Guardian Phone,Guardian Email,Guardian Address,Emergency Contact Name,Emergency Contact Phone,Class,Academic Year\n" +
                  "Ama,Mensah,2015-08-20,Female,Kofi Mensah,0240000000,kofi@example.com,Accra,Adwoa Mensah,0241111111,Grade 1,2026\n";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "scholarsync-student-import-template.csv");
    }

    private async Task<List<ImportRow>> ReadAndValidateAsync(string path)
    {
        var rows = new List<ImportRow>();
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            return rows;

        var headers = ParseCsvLine(headerLine).Select(NormalizeHeader).ToList();
        var requiredHeaders = new[] { "firstname", "lastname" };
        if (requiredHeaders.Any(header => !headers.Contains(header)))
        {
            ModelState.AddModelError(string.Empty, "The CSV must include First Name and Last Name columns. Download the template to see all supported columns.");
            return rows;
        }

        var classes = await _context.Classes.AsNoTracking().ToListAsync();
        var existingStudents = await _context.Students
            .IgnoreQueryFilters()
            .Where(student => !student.IsArchived)
            .AsNoTracking()
            .ToListAsync();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 1;

        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseCsvLine(line);
            var row = ImportRow.From(values, headers, lineNumber);
            ValidateRow(row, classes, existingStudents, seenKeys);
            rows.Add(row);
        }

        return rows;
    }

    private static void ValidateRow(ImportRow row, List<SchoolClass> classes, List<Student> existingStudents, HashSet<string> seenKeys)
    {
        if (string.IsNullOrWhiteSpace(row.FirstName)) row.Errors.Add("First Name is required.");
        if (string.IsNullOrWhiteSpace(row.LastName)) row.Errors.Add("Last Name is required.");
        if (!string.IsNullOrWhiteSpace(row.DateOfBirthText))
        {
            if (TryParseDate(row.DateOfBirthText, out var dateOfBirth))
                row.DateOfBirth = dateOfBirth;
            else
                row.Errors.Add("Date of Birth must be yyyy-MM-dd or dd/MM/yyyy.");
        }

        if (!string.IsNullOrWhiteSpace(row.ClassName))
        {
            var matchingClasses = classes.Where(schoolClass => string.Equals(schoolClass.Name.Trim(), row.ClassName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(row.AcademicYear))
                matchingClasses = matchingClasses.Where(schoolClass => string.Equals(schoolClass.AcademicYear.Trim(), row.AcademicYear.Trim(), StringComparison.OrdinalIgnoreCase));

            var matches = matchingClasses.ToList();
            if (matches.Count == 1)
                row.SchoolClassId = matches[0].Id;
            else if (matches.Count == 0)
                row.Errors.Add("Class and Academic Year do not match an existing class.");
            else
                row.Errors.Add("Academic Year is required when the class name exists in more than one year.");
        }
        else if (!string.IsNullOrWhiteSpace(row.AcademicYear))
        {
            row.Errors.Add("Academic Year can only be used when Class is provided.");
        }

        if (row.Errors.Count > 0)
            return;

        var key = DuplicateKey(row.FirstName, row.LastName, row.DateOfBirth, row.SchoolClassId);
        if (!seenKeys.Add(key))
            row.Errors.Add("This student appears more than once in the file.");

        if (existingStudents.Any(student => DuplicateKey(student.FirstName, student.LastName, student.DateOfBirth, student.SchoolClassId) == key))
            row.Errors.Add("An active student with the same name, date of birth, and class already exists.");
    }

    private static string DuplicateKey(string? firstName, string? lastName, DateTime? dateOfBirth, int? classId)
        => $"{Normalize(firstName)}|{Normalize(lastName)}|{dateOfBirth?.ToString("yyyy-MM-dd") ?? "no-dob"}|{classId?.ToString() ?? "unassigned"}";

    private static string Normalize(string? value) => string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private static string NormalizeHeader(string value) => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static bool TryParseDate(string value, out DateTime date)
        => DateTime.TryParseExact(value.Trim(), new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private string GetTempPath(string token) => Path.Combine(Path.GetTempPath(), "scholarsync-student-import-" + token);

    private static bool IsSafeToken(string token) => token.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) && token.Length == 36 && token[..^4].All(Uri.IsHexDigit);

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted) { values.Add(current.ToString().Trim()); current.Clear(); }
            else current.Append(character);
        }
        values.Add(current.ToString().Trim());
        return values;
    }

    public sealed class ImportRow
    {
        public int LineNumber { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string DateOfBirthText { get; init; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; init; } = string.Empty;
        public string GuardianName { get; init; } = string.Empty;
        public string GuardianPhone { get; init; } = string.Empty;
        public string GuardianEmail { get; init; } = string.Empty;
        public string GuardianAddress { get; init; } = string.Empty;
        public string EmergencyContactName { get; init; } = string.Empty;
        public string EmergencyContactPhone { get; init; } = string.Empty;
        public string ClassName { get; init; } = string.Empty;
        public string AcademicYear { get; init; } = string.Empty;
        public int? SchoolClassId { get; set; }
        public List<string> Errors { get; } = new();
        public bool IsValid => Errors.Count == 0;

        public Student ToStudent() => new()
        {
            FirstName = FirstName.Trim(), LastName = LastName.Trim(), DateOfBirth = DateOfBirth,
            Gender = EmptyToNull(Gender), GuardianName = EmptyToNull(GuardianName), GuardianPhone = EmptyToNull(GuardianPhone),
            GuardianEmail = EmptyToNull(GuardianEmail), GuardianAddress = EmptyToNull(GuardianAddress),
            EmergencyContactName = EmptyToNull(EmergencyContactName), EmergencyContactPhone = EmptyToNull(EmergencyContactPhone),
            SchoolClassId = SchoolClassId
        };

        public static ImportRow From(List<string> values, List<string> headers, int lineNumber)
        {
            string Get(string name)
            {
                var index = headers.IndexOf(name);
                return index >= 0 && index < values.Count ? values[index] : string.Empty;
            }

            return new ImportRow
            {
                LineNumber = lineNumber, FirstName = Get("firstname"), LastName = Get("lastname"), DateOfBirthText = Get("dateofbirth"),
                Gender = Get("gender"), GuardianName = Get("guardianname"), GuardianPhone = Get("guardianphone"), GuardianEmail = Get("guardianemail"),
                GuardianAddress = Get("guardianaddress"), EmergencyContactName = Get("emergencycontactname"), EmergencyContactPhone = Get("emergencycontactphone"),
                ClassName = Get("class"), AcademicYear = Get("academicyear")
            };
        }

        private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
