using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

[Authorize(Roles = "Admin")]
public class BackupModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly SchoolDbContext _context;
    private readonly AuditService _auditService;

    public BackupModel(IWebHostEnvironment environment, SchoolDbContext context, AuditService auditService)
    {
        _environment = environment;
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> OnGetDownloadAsync()
    {
        var databasePath = Path.Combine(_environment.ContentRootPath, "ScholarSync.db");
        if (!System.IO.File.Exists(databasePath))
            return NotFound("The ScholarSync database was not found.");

        var temporaryBackupPath = Path.Combine(Path.GetTempPath(), $"ScholarSync-backup-{Guid.NewGuid():N}.db");
        var connection = _context.Database.GetDbConnection();
        var closeConnection = connection.State != System.Data.ConnectionState.Open;

        try
        {
            if (closeConnection)
                await connection.OpenAsync();

            using var command = connection.CreateCommand();
            var safePath = temporaryBackupPath.Replace("'", "''");
            command.CommandText = $"VACUUM INTO '{safePath}';";
            await command.ExecuteNonQueryAsync();

            var bytes = await System.IO.File.ReadAllBytesAsync(temporaryBackupPath);
            var filename = $"ScholarSync-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db";
            await _auditService.LogAsync("Downloaded", "Backup", null, $"Downloaded {filename}.");
            return File(bytes, "application/x-sqlite3", filename);
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
            if (System.IO.File.Exists(temporaryBackupPath))
                System.IO.File.Delete(temporaryBackupPath);
        }
    }
}
