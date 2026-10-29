using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Security;

namespace ScholarSync.Pages.Account;

[Authorize(Roles = "Admin")]
[RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
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
    public bool RestoreTestPassed { get; private set; }
    public string? RestoreTestMessage { get; private set; }
    public int RestoreTestTableCount { get; private set; }
    public int RestoreTestStudentCount { get; private set; }
    public int RestoreTestClassCount { get; private set; }

    [BindProperty]
    public IFormFile? RestoreFile { get; set; }

    public async Task OnPostRestoreTestAsync()
    {
        if (RestoreFile is null || RestoreFile.Length == 0)
        {
            RestoreTestMessage = "Select a database backup file first.";
            return;
        }

        var extension = Path.GetExtension(RestoreFile.FileName).ToLowerInvariant();
        if (extension is not ".db" and not ".sqlite" and not ".sqlite3")
        {
            RestoreTestMessage = "Select a .db, .sqlite, or .sqlite3 backup file.";
            return;
        }

        if (RestoreFile.Length > 100 * 1024 * 1024)
        {
            RestoreTestMessage = "The backup file must be 100 MB or smaller.";
            return;
        }

        var temporaryPath = Path.Combine(Path.GetTempPath(), $"ScholarSync-restore-test-{Guid.NewGuid():N}.db");
        try
        {
            await using (var fileStream = System.IO.File.Create(temporaryPath))
                await RestoreFile.CopyToAsync(fileStream);

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = temporaryPath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            await using (var integrityCommand = connection.CreateCommand())
            {
                integrityCommand.CommandText = "PRAGMA integrity_check;";
                var integrityResult = Convert.ToString(await integrityCommand.ExecuteScalarAsync());
                if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    RestoreTestMessage = $"SQLite reported: {integrityResult}";
                    return;
                }
            }

            RestoreTestTableCount = await GetCountAsync(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';");
            RestoreTestStudentCount = await GetCountAsync(connection, "SELECT COUNT(*) FROM Students;");
            RestoreTestClassCount = await GetCountAsync(connection, "SELECT COUNT(*) FROM Classes;");
            RestoreTestPassed = true;
            RestoreTestMessage = "The backup opened successfully, passed SQLite integrity verification, and contains the expected school tables.";
            await _auditService.LogAsync("RestoreTested", "Backup", null, $"Tested backup {RestoreFile.FileName} successfully.");
        }
        catch (Exception exception) when (exception is SqliteException or IOException)
        {
            RestoreTestMessage = $"The backup could not be tested: {exception.Message}";
            await _auditService.LogAsync("RestoreTestFailed", "Backup", null, RestoreTestMessage);
        }
        finally
        {
            if (System.IO.File.Exists(temporaryPath))
                System.IO.File.Delete(temporaryPath);
        }
    }

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

    private static async Task<int> GetCountAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
