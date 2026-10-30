using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using ScholarSync.Data;
using ScholarSync.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Account");
    options.Conventions.AllowAnonymousToPage("/Error");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<ScholarSync.Security.EmailSender>();
builder.Services.AddScoped<ScholarSync.Security.AuditService>();
builder.Services.AddScoped<ScholarSync.Security.SchoolAccessService>();
builder.Services.AddScoped<ScholarSync.Security.AcademicYearService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Apply any pending EF Core migrations when the application starts.
// This keeps the local SQLite schema aligned with the current data model.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();
    db.Database.Migrate();

    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS AttendanceRecords (
            Id INTEGER NOT NULL CONSTRAINT PK_AttendanceRecords PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            AttendanceDate TEXT NOT NULL,
            Status TEXT NOT NULL,
            CONSTRAINT FK_AttendanceRecords_Students_StudentId
                FOREIGN KEY (StudentId) REFERENCES Students (Id) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS IX_AttendanceRecords_StudentId_AttendanceDate
            ON AttendanceRecords (StudentId, AttendanceDate);

        CREATE TABLE IF NOT EXISTS GradeRecords (
            Id INTEGER NOT NULL CONSTRAINT PK_GradeRecords PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            Subject TEXT NOT NULL,
            Score TEXT NOT NULL,
            RecordedAt TEXT NOT NULL,
            CONSTRAINT FK_GradeRecords_Students_StudentId
                FOREIGN KEY (StudentId) REFERENCES Students (Id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS AdminUsers (
            Id INTEGER NOT NULL CONSTRAINT PK_AdminUsers PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL,
            Email TEXT NOT NULL DEFAULT '',
            Role TEXT NOT NULL DEFAULT 'Admin',
            PasswordHash TEXT NOT NULL,
            PasswordSalt TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS IX_AdminUsers_Username ON AdminUsers (Username);

        CREATE TABLE IF NOT EXISTS PasswordResetTokens (
            Id INTEGER NOT NULL CONSTRAINT PK_PasswordResetTokens PRIMARY KEY AUTOINCREMENT,
            AdminUserId INTEGER NOT NULL,
            TokenHash TEXT NOT NULL,
            ExpiresAt TEXT NOT NULL,
            UsedAt TEXT NULL,
            CONSTRAINT FK_PasswordResetTokens_AdminUsers_AdminUserId
                FOREIGN KEY (AdminUserId) REFERENCES AdminUsers (Id) ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS IX_PasswordResetTokens_TokenHash ON PasswordResetTokens (TokenHash);

        CREATE TABLE IF NOT EXISTS AuditLogs (
            Id INTEGER NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL,
            Action TEXT NOT NULL,
            EntityType TEXT NOT NULL,
            EntityId INTEGER NULL,
            Details TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_AuditLogs_CreatedAt ON AuditLogs (CreatedAt);

        CREATE TABLE IF NOT EXISTS StudentEnrollments (
            Id INTEGER NOT NULL CONSTRAINT PK_StudentEnrollments PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            SchoolClassId INTEGER NOT NULL,
            AcademicYear TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            CONSTRAINT FK_StudentEnrollments_Students_StudentId FOREIGN KEY (StudentId) REFERENCES Students (Id) ON DELETE CASCADE,
            CONSTRAINT FK_StudentEnrollments_Classes_SchoolClassId FOREIGN KEY (SchoolClassId) REFERENCES Classes (Id) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS IX_StudentEnrollments_StudentId_AcademicYear ON StudentEnrollments (StudentId, AcademicYear);

        CREATE TABLE IF NOT EXISTS ClassTeachers (
            Id INTEGER NOT NULL CONSTRAINT PK_ClassTeachers PRIMARY KEY AUTOINCREMENT,
            SchoolClassId INTEGER NOT NULL,
            AdminUserId INTEGER NOT NULL,
            AssignedAt TEXT NOT NULL,
            CONSTRAINT FK_ClassTeachers_Classes_SchoolClassId FOREIGN KEY (SchoolClassId) REFERENCES Classes (Id) ON DELETE CASCADE,
            CONSTRAINT FK_ClassTeachers_AdminUsers_AdminUserId FOREIGN KEY (AdminUserId) REFERENCES AdminUsers (Id) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS IX_ClassTeachers_SchoolClassId ON ClassTeachers (SchoolClassId);

        CREATE TABLE IF NOT EXISTS FeePayments (
            Id INTEGER NOT NULL CONSTRAINT PK_FeePayments PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER NOT NULL,
            Term TEXT NOT NULL,
            AcademicYear TEXT NOT NULL,
            Amount TEXT NOT NULL,
            ReceiptNumber TEXT NULL,
            PaymentDate TEXT NOT NULL,
            Notes TEXT NULL,
            CONSTRAINT FK_FeePayments_Students_StudentId FOREIGN KEY (StudentId) REFERENCES Students (Id) ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS IX_FeePayments_PaymentDate ON FeePayments (PaymentDate);

        CREATE TABLE IF NOT EXISTS FeeStructures (
            Id INTEGER NOT NULL CONSTRAINT PK_FeeStructures PRIMARY KEY AUTOINCREMENT,
            SchoolClassId INTEGER NOT NULL,
            Term TEXT NOT NULL,
            AcademicYear TEXT NOT NULL,
            Amount TEXT NOT NULL,
            Description TEXT NULL,
            CONSTRAINT FK_FeeStructures_Classes_SchoolClassId FOREIGN KEY (SchoolClassId) REFERENCES Classes (Id) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS IX_FeeStructures_Class_Term_Year ON FeeStructures (SchoolClassId, Term, AcademicYear);

        CREATE TABLE IF NOT EXISTS SchoolSettings (
            Id INTEGER NOT NULL CONSTRAINT PK_SchoolSettings PRIMARY KEY,
            SchoolName TEXT NOT NULL,
            AcademicYear TEXT NOT NULL,
            WelcomeMessage TEXT NOT NULL,
            SchoolAddress TEXT NOT NULL DEFAULT '',
            SchoolPhone TEXT NOT NULL DEFAULT '',
            SchoolEmail TEXT NOT NULL DEFAULT '',
            PrimaryColor TEXT NOT NULL,
            AccentColor TEXT NOT NULL
        );
        INSERT INTO SchoolSettings (Id, SchoolName, AcademicYear, WelcomeMessage, PrimaryColor, AccentColor)
        SELECT 1, 'ScholarSync School', '2026', 'Welcome back. Here’s what’s happening today.', '#142B4A', '#40D6C2'
        WHERE NOT EXISTS (SELECT 1 FROM SchoolSettings WHERE Id = 1);
        """);

    var connection = db.Database.GetDbConnection();
    var closeConnection = connection.State != System.Data.ConnectionState.Open;
    if (closeConnection)
        connection.Open();
    var hasEmailColumn = false;
    using (var command = connection.CreateCommand())
    {
        command.CommandText = "PRAGMA table_info(AdminUsers);";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            hasEmailColumn |= reader.GetString(1).Equals("Email", StringComparison.OrdinalIgnoreCase);
    }
    if (!hasEmailColumn)
        db.Database.ExecuteSqlRaw("ALTER TABLE AdminUsers ADD COLUMN Email TEXT NOT NULL DEFAULT '';");

    var hasRoleColumn = false;
    using (var command = connection.CreateCommand())
    {
        command.CommandText = "PRAGMA table_info(AdminUsers);";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            hasRoleColumn |= reader.GetString(1).Equals("Role", StringComparison.OrdinalIgnoreCase);
    }
    if (!hasRoleColumn)
        db.Database.ExecuteSqlRaw("ALTER TABLE AdminUsers ADD COLUMN Role TEXT NOT NULL DEFAULT 'Admin';");

    var hasLogoPathColumn = false;
    using (var command = connection.CreateCommand())
    {
        command.CommandText = "PRAGMA table_info(SchoolSettings);";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            hasLogoPathColumn |= reader.GetString(1).Equals("LogoPath", StringComparison.OrdinalIgnoreCase);
    }
    if (!hasLogoPathColumn)
        db.Database.ExecuteSqlRaw("ALTER TABLE SchoolSettings ADD COLUMN LogoPath TEXT NOT NULL DEFAULT ''; ");

    var schoolSettingsColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    using (var command = connection.CreateCommand())
    {
        command.CommandText = "PRAGMA table_info(SchoolSettings);";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            schoolSettingsColumns.Add(reader.GetString(1));
    }
    if (!schoolSettingsColumns.Contains("SchoolAddress"))
        db.Database.ExecuteSqlRaw("ALTER TABLE SchoolSettings ADD COLUMN SchoolAddress TEXT NOT NULL DEFAULT ''; ");
    if (!schoolSettingsColumns.Contains("SchoolPhone"))
        db.Database.ExecuteSqlRaw("ALTER TABLE SchoolSettings ADD COLUMN SchoolPhone TEXT NOT NULL DEFAULT ''; ");
    if (!schoolSettingsColumns.Contains("SchoolEmail"))
        db.Database.ExecuteSqlRaw("ALTER TABLE SchoolSettings ADD COLUMN SchoolEmail TEXT NOT NULL DEFAULT ''; ");

    var studentColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    using (var command = connection.CreateCommand())
    {
        command.CommandText = "PRAGMA table_info(Students);";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            studentColumns.Add(reader.GetString(1));
    }
    if (!studentColumns.Contains("GuardianEmail"))
        db.Database.ExecuteSqlRaw("ALTER TABLE Students ADD COLUMN GuardianEmail TEXT NULL;");
    if (!studentColumns.Contains("GuardianAddress"))
        db.Database.ExecuteSqlRaw("ALTER TABLE Students ADD COLUMN GuardianAddress TEXT NULL;");
    if (!studentColumns.Contains("EmergencyContactName"))
        db.Database.ExecuteSqlRaw("ALTER TABLE Students ADD COLUMN EmergencyContactName TEXT NULL;");
    if (!studentColumns.Contains("EmergencyContactPhone"))
        db.Database.ExecuteSqlRaw("ALTER TABLE Students ADD COLUMN EmergencyContactPhone TEXT NULL;");
    if (closeConnection)
        connection.Close();

    var academicYear = DateTime.UtcNow.Year.ToString();
    var defaultClassNames = new[]
    {
        "Creche",
        "KG 1",
        "KG 2",
        "Primary 1",
        "Primary 2",
        "Primary 3",
        "Primary 4",
        "Primary 5",
        "Primary 6",
        "Grade 7",
        "Grade 8",
        "Grade 9"
    };

    foreach (var className in defaultClassNames)
    {
        if (!db.Classes.Any(schoolClass =>
                schoolClass.Name == className && schoolClass.AcademicYear == academicYear))
        {
            db.Classes.Add(new SchoolClass
            {
                Name = className,
                AcademicYear = academicYear
            });
        }
    }

    db.SaveChanges();

    // Merge legacy JHS labels into Grade 7–9 while preserving linked records.
    var legacyClassMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["JHS 1"] = "Grade 7",
        ["JHS 2"] = "Grade 8",
        ["JHS 3"] = "Grade 9"
    };
    foreach (var mapping in legacyClassMappings)
    {
        var legacyClasses = db.Classes
            .Where(schoolClass => schoolClass.Name.ToLower() == mapping.Key.ToLower())
            .ToList();
        foreach (var legacyClass in legacyClasses)
        {
            var targetClass = db.Classes.FirstOrDefault(schoolClass =>
                schoolClass.Id != legacyClass.Id &&
                schoolClass.Name == mapping.Value &&
                schoolClass.AcademicYear == legacyClass.AcademicYear);

            if (targetClass == null)
            {
                legacyClass.Name = mapping.Value;
                continue;
            }

            foreach (var student in db.Students.Where(student => student.SchoolClassId == legacyClass.Id).ToList())
                student.SchoolClassId = targetClass.Id;

            foreach (var enrollment in db.StudentEnrollments.Where(enrollment => enrollment.SchoolClassId == legacyClass.Id).ToList())
            {
                var targetEnrollmentExists = db.StudentEnrollments.Any(existing =>
                    existing.Id != enrollment.Id &&
                    existing.StudentId == enrollment.StudentId &&
                    existing.AcademicYear == enrollment.AcademicYear);
                if (targetEnrollmentExists)
                    db.StudentEnrollments.Remove(enrollment);
                else
                    enrollment.SchoolClassId = targetClass.Id;
            }

            foreach (var assignment in db.ClassTeachers.Where(assignment => assignment.SchoolClassId == legacyClass.Id).ToList())
            {
                if (db.ClassTeachers.Any(existing => existing.SchoolClassId == targetClass.Id))
                    db.ClassTeachers.Remove(assignment);
                else
                    assignment.SchoolClassId = targetClass.Id;
            }

            foreach (var structure in db.FeeStructures.Where(structure => structure.SchoolClassId == legacyClass.Id).ToList())
            {
                var targetStructureExists = db.FeeStructures.Any(existing =>
                    existing.Id != structure.Id &&
                    existing.SchoolClassId == targetClass.Id &&
                    existing.Term == structure.Term &&
                    existing.AcademicYear == structure.AcademicYear);
                if (targetStructureExists)
                    db.FeeStructures.Remove(structure);
                else
                    structure.SchoolClassId = targetClass.Id;
            }

            db.Classes.Remove(legacyClass);
        }
    }
    db.SaveChanges();

    // Repair duplicate class rows created before the unique class rule existed.
    var duplicateClassGroups = db.Classes
        .ToList()
        .GroupBy(schoolClass => new { schoolClass.Name, schoolClass.AcademicYear })
        .Where(group => group.Count() > 1)
        .ToList();
    foreach (var duplicateGroup in duplicateClassGroups)
    {
        var keep = duplicateGroup.OrderBy(schoolClass => schoolClass.Id).First();
        var duplicateIds = duplicateGroup.Where(schoolClass => schoolClass.Id != keep.Id).Select(schoolClass => schoolClass.Id).ToList();
        var affectedStudents = db.Students.Where(student => student.SchoolClassId.HasValue && duplicateIds.Contains(student.SchoolClassId.Value)).ToList();
        foreach (var student in affectedStudents)
            student.SchoolClassId = keep.Id;
        db.Classes.RemoveRange(duplicateGroup.Where(schoolClass => schoolClass.Id != keep.Id));
    }
    db.SaveChanges();
    db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Classes_Name_AcademicYear ON Classes (Name, AcademicYear);");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
