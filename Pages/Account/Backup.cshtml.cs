using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
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

    public bool BackupVerified { get; private set; }
    public string? VerificationMessage { get; private set; }
    public long? BackupSizeBytes { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }

    public async Task OnGetVerifyAsync()
    {
        var databasePath = Path.Combine(_environment.ContentRootPath, "ScholarSync.db");
        if (!System.IO.File.Exists(databasePath))
        {
            VerificationMessage = "The ScholarSync database was not found.";
            return;
        }

        var temporaryBackupPath = Path.Combine(Path.GetTempPath(), $"ScholarSync-verify-{Guid.NewGuid():N}.db");
        try
        {
            await CreateBackupAsync(temporaryBackupPath);
            await using var verificationConnection = new SqliteConnection($"Data Source={temporaryBackupPath};Mode=ReadOnly");
            await verificationConnection.OpenAsync();
            await using var command = verificationConnection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = Convert.ToString(await command.ExecuteScalarAsync());

            BackupVerified = string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
            VerificationMessage = BackupVerified
                ? "The backup was created successfully and passed SQLite integrity verification."
                : $"SQLite reported: {result}";
            var fileInfo = new FileInfo(temporaryBackupPath);
            BackupSizeBytes = fileInfo.Length;
            VerifiedAtUtc = DateTime.UtcNow;
            await _auditService.LogAsync(BackupVerified ? "Verified" : "VerificationFailed", "Backup", null, VerificationMessage);
        }
        catch (Exception exception) when (exception is SqliteException or IOException)
        {
            VerificationMessage = $"The backup could not be verified: {exception.Message}";
            await _auditService.LogAsync("VerificationFailed", "Backup", null, VerificationMessage);
        }
        finally
        {
            if (System.IO.File.Exists(temporaryBackupPath))
                System.IO.File.Delete(temporaryBackupPath);
        }
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

            await CreateBackupAsync(temporaryBackupPath);

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

    private async Task CreateBackupAsync(string backupPath)
    {
        var connection = _context.Database.GetDbConnection();
        var closeConnection = connection.State != System.Data.ConnectionState.Open;
        try
        {
            if (closeConnection)
                await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            var safePath = backupPath.Replace("'", "''");
            command.CommandText = $"VACUUM INTO '{safePath}';";
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }
    }
}
