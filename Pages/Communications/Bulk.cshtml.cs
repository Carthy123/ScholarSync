using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Communications;

public class BulkModel : PageModel
{
    private const int MaxRecipients = 200;
    private static readonly string[] Channels = { "In-app", "Email", "SMS", "WhatsApp" };
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;
    private readonly AuditService _auditService;
    private readonly EmailSender _emailSender;
    private readonly TwilioMessagingSender _twilioMessagingSender;
    private readonly IConfiguration _configuration;

    public BulkModel(SchoolDbContext context, SchoolAccessService accessService, AuditService auditService, EmailSender emailSender, TwilioMessagingSender twilioMessagingSender, IConfiguration configuration)
    {
        _context = context;
        _accessService = accessService;
        _auditService = auditService;
        _emailSender = emailSender;
        _twilioMessagingSender = twilioMessagingSender;
        _configuration = configuration;
    }

    [BindProperty]
    public BulkInput Input { get; set; } = new();

    public IList<SchoolClass> Classes { get; private set; } = new List<SchoolClass>();
    public IList<CommunicationTemplate> Templates { get; private set; } = new List<CommunicationTemplate>();
    public IReadOnlyList<string> AvailableChannels => Channels;

    public async Task OnGetAsync() => await LoadClassesAsync();

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

        var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
        if (!_accessService.IsAdmin && !allowedClassIds.Contains(Input.ClassId))
            return Forbid();

        var students = await _context.Students
            .Where(student => student.SchoolClassId == Input.ClassId)
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();

        if (students.Count == 0)
            ModelState.AddModelError("Input.ClassId", "The selected class has no active students.");
        if (students.Count > MaxRecipients)
            ModelState.AddModelError("Input.ClassId", $"This class has more than {MaxRecipients} students. Use smaller groups to protect delivery limits.");

        if (!ModelState.IsValid)
        {
            await LoadClassesAsync();
            return Page();
        }

        var callbackUrl = _configuration.GetValue<string>("Twilio:StatusCallbackUrl");
        var selectedChannel = Channels.First(channel => channel.Equals(Input.Channel, StringComparison.OrdinalIgnoreCase));
        var saved = 0;
        var pendingOrFailed = 0;

        foreach (var student in students)
        {
            var communication = new GuardianCommunication
            {
                StudentId = student.Id,
                RecipientName = student.GuardianName?.Trim() ?? "Guardian details not provided",
                RecipientEmail = student.GuardianEmail?.Trim(),
                RecipientPhone = student.GuardianPhone?.Trim(),
                Channel = selectedChannel,
                Subject = Input.Subject.Trim(),
                Message = Input.Message.Trim(),
                CreatedBy = User.Identity?.Name
            };

            await PrepareDeliveryAsync(communication, callbackUrl);
            if (communication.Status is "Pending" or "Failed") pendingOrFailed++;
            _context.GuardianCommunications.Add(communication);
            saved++;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "GuardianCommunication", details: $"Sent bulk {selectedChannel} communication to {saved} guardian record(s) in class {Input.ClassId}; {pendingOrFailed} pending or failed.");
        TempData["CommunicationSaved"] = $"Bulk communication saved for {saved} student(s). {pendingOrFailed} message(s) are pending or failed.";
        return RedirectToPage("/Communications/Index");
    }

    private async Task PrepareDeliveryAsync(GuardianCommunication communication, string? callbackUrl)
    {
        if (communication.Channel == "In-app")
        {
            communication.Status = "Recorded";
            communication.StatusDetails = "Saved in the school communication history.";
            return;
        }

        if (communication.Channel == "Email")
        {
            if (string.IsNullOrWhiteSpace(communication.RecipientEmail))
            {
                communication.Status = "Failed";
                communication.StatusDetails = "Guardian email address is missing.";
                return;
            }

            try
            {
                var sent = await _emailSender.SendCommunicationAsync(communication.RecipientEmail, communication.Subject, communication.Message);
                communication.Status = sent ? "Sent" : "Pending";
                communication.StatusDetails = sent ? "Email accepted by the configured SMTP server." : "SMTP is not configured yet.";
                communication.SentAt = sent ? DateTime.UtcNow : null;
            }
            catch (Exception exception)
            {
                communication.Status = "Failed";
                communication.StatusDetails = $"Email could not be sent: {exception.Message[..Math.Min(exception.Message.Length, 250)]}";
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(communication.RecipientPhone))
        {
            communication.Status = "Failed";
            communication.StatusDetails = "Guardian phone number is missing.";
            return;
        }

        var delivery = await _twilioMessagingSender.SendAsync(communication.Channel, communication.RecipientPhone, communication.Message, callbackUrl);
        communication.Status = delivery.Sent ? "Sent" : "Pending";
        communication.StatusDetails = delivery.Details;
        communication.ProviderMessageId = delivery.ProviderMessageId;
        communication.SentAt = delivery.Sent ? DateTime.UtcNow : null;
    }

    private async Task LoadClassesAsync()
    {
        Classes = await _context.Classes.AsNoTracking().ToListAsync();
        if (!_accessService.IsAdmin)
        {
            var allowedClassIds = await _accessService.GetAllowedClassIdsAsync();
            Classes = Classes.Where(schoolClass => allowedClassIds.Contains(schoolClass.Id)).ToList();
        }
        Classes = ClassOrdering.Order(Classes).ToList();
        Templates = await _context.CommunicationTemplates.AsNoTracking().OrderBy(template => template.Name).ToListAsync();
    }

    public class BulkInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a class.")] public int ClassId { get; set; }
        public int? TemplateId { get; set; }
        [Required] public string Channel { get; set; } = "In-app";
        [Required, StringLength(150)] public string Subject { get; set; } = string.Empty;
        [Required, StringLength(4000)] public string Message { get; set; } = string.Empty;
    }
}
