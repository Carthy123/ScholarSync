using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Communications;

public class IndexModel : PageModel
{
    private static readonly string[] Channels = { "In-app", "Email", "SMS", "WhatsApp" };
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;
    private readonly AuditService _auditService;
    private readonly EmailSender _emailSender;
    private readonly TwilioMessagingSender _twilioMessagingSender;
    private readonly IConfiguration _configuration;

    public IndexModel(SchoolDbContext context, SchoolAccessService accessService, AuditService auditService, EmailSender emailSender, TwilioMessagingSender twilioMessagingSender, IConfiguration configuration)
    {
        _context = context;
        _accessService = accessService;
        _auditService = auditService;
        _emailSender = emailSender;
        _twilioMessagingSender = twilioMessagingSender;
        _configuration = configuration;
    }

    [BindProperty]
    public CommunicationInput Input { get; set; } = new();

    public IList<Student> Students { get; private set; } = new List<Student>();
    public IList<GuardianCommunication> Communications { get; private set; } = new List<GuardianCommunication>();
    public IList<CommunicationTemplate> Templates { get; private set; } = new List<CommunicationTemplate>();
    public IReadOnlyList<string> AvailableChannels => Channels;

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostSendAsync()
    {
        if (Input.TemplateId.HasValue)
        {
            var template = await _context.CommunicationTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.Id == Input.TemplateId.Value);
            if (template == null) ModelState.AddModelError("Input.TemplateId", "The selected template no longer exists.");
            else { Input.Channel = template.Channel; Input.Subject = template.Subject; Input.Message = template.Message; ModelState.Remove("Input.Subject"); ModelState.Remove("Input.Message"); }
        }
        if (!Channels.Contains(Input.Channel, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError("Input.Channel", "Select a valid communication channel.");

        var student = await _context.Students.Include(item => item.SchoolClass).FirstOrDefaultAsync(item => item.Id == Input.StudentId);
        if (student == null || !await _accessService.CanAccessStudentAsync(Input.StudentId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(student.GuardianName))
            ModelState.AddModelError("Input.StudentId", "This student does not have a guardian name recorded yet.");
        if (student.GuardianCommunicationOptOut)
            ModelState.AddModelError("Input.StudentId", "This guardian has opted out of communications.");
        if (string.Equals(Input.Channel, "Email", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(student.GuardianEmail))
            ModelState.AddModelError("Input.Channel", "This guardian does not have an email address recorded.");
        if ((string.Equals(Input.Channel, "SMS", StringComparison.OrdinalIgnoreCase) || string.Equals(Input.Channel, "WhatsApp", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrWhiteSpace(student.GuardianPhone))
            ModelState.AddModelError("Input.Channel", "This guardian does not have a phone number recorded.");

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var communication = new GuardianCommunication
        {
            StudentId = student.Id,
            RecipientName = student.GuardianName!.Trim(),
            RecipientEmail = student.GuardianEmail?.Trim(),
            RecipientPhone = student.GuardianPhone?.Trim(),
            Channel = Channels.First(channel => channel.Equals(Input.Channel, StringComparison.OrdinalIgnoreCase)),
            Subject = Input.Subject.Trim(),
            Message = Input.Message.Trim(),
            CreatedBy = User.Identity?.Name
        };

        if (communication.Channel == "In-app")
        {
            communication.Status = "Recorded";
            communication.StatusDetails = "Saved in the school communication history.";
        }
        else if (communication.Channel == "Email")
        {
            try
            {
                var sent = await _emailSender.SendCommunicationAsync(communication.RecipientEmail!, communication.Subject, communication.Message);
                communication.Status = sent ? "Sent" : "Pending";
                communication.StatusDetails = sent ? "Email accepted by the configured SMTP server." : "SMTP is not configured yet.";
                communication.SentAt = sent ? DateTime.UtcNow : null;
            }
            catch (Exception exception)
            {
                communication.Status = "Failed";
                communication.StatusDetails = $"Email could not be sent: {exception.Message[..Math.Min(exception.Message.Length, 250)]}";
            }
        }
        else
        {
            var callbackUrl = _configuration.GetValue<string>("Twilio:StatusCallbackUrl");
            var delivery = await _twilioMessagingSender.SendAsync(communication.Channel, communication.RecipientPhone!, communication.Message, callbackUrl);
            communication.Status = delivery.Sent ? "Sent" : "Pending";
            communication.StatusDetails = delivery.Details;
            communication.ProviderMessageId = delivery.ProviderMessageId;
            communication.SentAt = delivery.Sent ? DateTime.UtcNow : null;
        }

        _context.GuardianCommunications.Add(communication);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "GuardianCommunication", communication.Id, $"Created {communication.Channel} communication for {communication.RecipientName} about {student.FirstName} {student.LastName}; status: {communication.Status}.");
        TempData["CommunicationSaved"] = $"Communication saved with status: {communication.Status}.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        var studentsQuery = _context.Students.Include(student => student.SchoolClass).AsNoTracking().OrderBy(student => student.LastName).ThenBy(student => student.FirstName);
        if (!_accessService.IsAdmin)
            studentsQuery = studentsQuery.Where(student => student.SchoolClassId.HasValue && allowedClassIds.Contains(student.SchoolClassId.Value)).OrderBy(student => student.LastName).ThenBy(student => student.FirstName);
        Students = await studentsQuery.ToListAsync();

        var communicationsQuery = _context.GuardianCommunications.Include(item => item.Student).AsNoTracking().OrderByDescending(item => item.CreatedAt).Take(100);
        if (!_accessService.IsAdmin)
            communicationsQuery = communicationsQuery.Where(item => item.Student.SchoolClassId.HasValue && allowedClassIds.Contains(item.Student.SchoolClassId.Value)).OrderByDescending(item => item.CreatedAt).Take(100);
        Communications = await communicationsQuery.ToListAsync();
        Templates = await _context.CommunicationTemplates.AsNoTracking().OrderBy(template => template.Name).ToListAsync();
    }

    public class CommunicationInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a student.")] public int StudentId { get; set; }
        public int? TemplateId { get; set; }
        [Required] public string Channel { get; set; } = "In-app";
        [Required, StringLength(150)] public string Subject { get; set; } = string.Empty;
        [Required, StringLength(4000)] public string Message { get; set; } = string.Empty;
    }
}
