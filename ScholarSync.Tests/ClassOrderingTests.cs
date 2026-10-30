using ScholarSync.Models;
using ScholarSync.Security;
using Xunit;

namespace ScholarSync.Tests;

public class ClassOrderingTests
{
    [Fact]
    public void Order_ReturnsGhanaianProgressionInsteadOfAlphabeticalOrder()
    {
        var classes = new[]
        {
            new SchoolClass { Name = "Grade 9" },
            new SchoolClass { Name = "Grade 2" },
            new SchoolClass { Name = "Creche" },
            new SchoolClass { Name = "KG 1" },
            new SchoolClass { Name = "Grade 7" },
            new SchoolClass { Name = "Grade 1" }
        };

        var orderedNames = ClassOrdering.Order(classes).Select(schoolClass => schoolClass.Name).ToArray();

        Assert.Equal(new[] { "Creche", "KG 1", "Grade 1", "Grade 2", "Grade 7", "Grade 9" }, orderedNames);
    }

    [Fact]
    public void GetOrder_TreatsLegacyJhsNamesAsGradeNames()
    {
        Assert.Equal(ClassOrdering.GetOrder("Grade 7"), ClassOrdering.GetOrder("JHS 1"));
        Assert.Equal(ClassOrdering.GetOrder("Grade 8"), ClassOrdering.GetOrder("JHS 2"));
        Assert.Equal(ClassOrdering.GetOrder("Grade 9"), ClassOrdering.GetOrder("JHS 3"));
    }
}
