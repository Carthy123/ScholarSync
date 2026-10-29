using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;

namespace ScholarSync.Pages.Fees;

public class ReceiptModel : PageModel
{
    private readonly SchoolDbContext _context;
    private readonly SchoolAccessService _accessService;

    public ReceiptModel(SchoolDbContext context, SchoolAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    public FeePayment Payment { get; private set; } = null!;
    public SchoolSettings Settings { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var payment = await _context.FeePayments
            .Include(payment => payment.Student)
            .ThenInclude(student => student.SchoolClass)
            .AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.Id == id);
        if (payment == null)
            return NotFound();
        if (!await _accessService.CanAccessStudentAsync(payment.StudentId))
            return Forbid();
        Payment = payment;

        Settings = await _context.SchoolSettings.AsNoTracking().FirstOrDefaultAsync(settings => settings.Id == 1) ?? new SchoolSettings();
        return Page();
    }
}
