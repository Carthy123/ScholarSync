namespace ScholarSync.Models;

public static class GradingScale
{
    public static string GetGrade(decimal score, SchoolSettings settings)
    {
        if (score >= settings.GradeAThreshold) return "A";
        if (score >= settings.GradeBThreshold) return "B";
        if (score >= settings.GradeCThreshold) return "C";
        if (score >= settings.GradeDThreshold) return "D";
        return "F";
    }
}
