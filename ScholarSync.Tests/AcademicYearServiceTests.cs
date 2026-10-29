using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ScholarSync.Data;
using ScholarSync.Models;
using ScholarSync.Security;
using Xunit;

namespace ScholarSync.Tests;

public class AcademicYearServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_ReturnsConfiguredSchoolYear()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SchoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SchoolDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.SchoolSettings.Add(new SchoolSettings { Id = 1, AcademicYear = "2027" });
        await context.SaveChangesAsync();

        var service = new AcademicYearService(context);

        var result = await service.GetCurrentAsync();

        Assert.Equal("2027", result);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsCurrentYearWhenSettingIsMissing()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SchoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SchoolDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var service = new AcademicYearService(context);

        var result = await service.GetCurrentAsync();

        Assert.Equal(DateTime.UtcNow.Year.ToString(), result);
    }
}
