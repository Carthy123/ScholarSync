using ScholarSync.Models;

namespace ScholarSync.Security;

public static class ClassOrdering
{
    public static int GetOrder(string? className)
    {
        var normalized = new string((className ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

        return normalized switch
        {
            "CRECHE" => 0,
            "KG1" => 1,
            "KG2" => 2,
            "PRIMARY1" => 3,
            "PRIMARY2" => 4,
            "PRIMARY3" => 5,
            "PRIMARY4" => 6,
            "PRIMARY5" => 7,
            "PRIMARY6" => 8,
            "GRADE7" or "JHS1" => 9,
            "GRADE8" or "JHS2" => 10,
            "GRADE9" or "JHS3" => 11,
            _ => 1000
        };
    }

    public static IEnumerable<SchoolClass> Order(IEnumerable<SchoolClass> classes) =>
        classes.OrderBy(schoolClass => GetOrder(schoolClass.Name))
            .ThenBy(schoolClass => schoolClass.Name)
            .ThenBy(schoolClass => schoolClass.AcademicYear);
}
