using ScholarSync.Models;

namespace ScholarSync;

public static class StudentStore
{
    public static List<Student> Students { get; } = new()
    {
        new Student
        {
            Id = 1,
            FirstName = "Test",
            LastName = "Student",
            Gender = "Male"
        }
    };
}