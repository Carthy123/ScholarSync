using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Communications;

[Authorize(Roles = "Admin")]
public class TemplatesModel : PageModel
{
    private static readonly string[] Channels = { "In-app", "Email", "SMS", "WhatsApp" };
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public TemplatesModel(SchoolDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [BindProperty]
    public TemplateInput Input { get; set; } = new();

    public IList<CommunicationTemplate> Templates { get; private set; } = new List<CommunicationTemplate>();
    public IReadOnlyList<string> AvailableChannels => Channels;

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!Channels.Contains(Input.Channel, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError("Input.Channel", "Select a valid channel.");
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var template = new CommunicationTemplate
        {
            Name = Input.Name.Trim(), Category = Input.Category.Trim(), Channel = Channels.First(channel => channel.Equals(Input.Channel, StringComparison.OrdinalIgnoreCase)),
            Subject = Input.Subject.Trim(), Message = Input.Message.Trim(), CreatedBy = User.Identity?.Name
        };
        _context.CommunicationTemplates.Add(template);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Created", "CommunicationTemplate", template.Id, $"Created {template.Channel} communication template '{template.Name}'.");
        TempData["TemplateSaved"] = "Communication template created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var template = await _context.CommunicationTemplates.FindAsync(id);
        if (template != null)
        {
            _context.CommunicationTemplates.Remove(template);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Deleted", "CommunicationTemplate", id, $"Deleted communication template '{template.Name}'.");
        }
        return RedirectToPage();
    }

    private async Task LoadAsync() => Templates = await _context.CommunicationTemplates.AsNoTracking().OrderBy(template => template.Category).ThenBy(template => template.Name).ToListAsync();

    public class TemplateInput
    {
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [Required, StringLength(30)] public string Category { get; set; } = "General";
        [Required] public string Channel { get; set; } = "In-app";
        [Required, StringLength(150)] public string Subject { get; set; } = string.Empty;
        [Required, StringLength(4000)] public string Message { get; set; } = string.Empty;
    }
}
