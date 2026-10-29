using Microsoft.EntityFrameworkCore;
using ScholarSync.Models;

namespace ScholarSync.Data;

public class SchoolDbContext : DbContext
{
    public SchoolDbContext(
        DbContextOptions<SchoolDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<GradeRecord> GradeRecords => Set<GradeRecord>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<SchoolSettings> SchoolSettings => Set<SchoolSettings>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<ClassTeacher> ClassTeachers => Set<ClassTeacher>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
}
