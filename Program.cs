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
            PasswordHash TEXT NOT NULL,
            PasswordSalt TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS IX_AdminUsers_Username ON AdminUsers (Username);

        CREATE TABLE IF NOT EXISTS SchoolSettings (
            Id INTEGER NOT NULL CONSTRAINT PK_SchoolSettings PRIMARY KEY,
            SchoolName TEXT NOT NULL,
            AcademicYear TEXT NOT NULL,
            WelcomeMessage TEXT NOT NULL,
            PrimaryColor TEXT NOT NULL,
            AccentColor TEXT NOT NULL
        );
        INSERT INTO SchoolSettings (Id, SchoolName, AcademicYear, WelcomeMessage, PrimaryColor, AccentColor)
        SELECT 1, 'ScholarSync School', '2026', 'Welcome back. Here’s what’s happening today.', '#142B4A', '#40D6C2'
        WHERE NOT EXISTS (SELECT 1 FROM SchoolSettings WHERE Id = 1);
        """);

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
        "JHS 1",
        "JHS 2",
        "JHS 3"
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
