using ScholarSync.Security;
using Xunit;

namespace ScholarSync.Tests;

public class PasswordServiceTests
{
    [Fact]
    public void HashPassword_CreatesDifferentSaltAndVerifiesCorrectPassword()
    {
        var first = PasswordService.HashPassword("CorrectPassword123!");
        var second = PasswordService.HashPassword("CorrectPassword123!");

        Assert.NotEqual(first.Salt, second.Salt);
        Assert.True(PasswordService.VerifyPassword("CorrectPassword123!", first.Hash, first.Salt));
        Assert.True(PasswordService.VerifyPassword("CorrectPassword123!", second.Hash, second.Salt));
    }

    [Fact]
    public void VerifyPassword_RejectsIncorrectPassword()
    {
        var password = PasswordService.HashPassword("CorrectPassword123!");

        Assert.False(PasswordService.VerifyPassword("WrongPassword123!", password.Hash, password.Salt));
    }
}
