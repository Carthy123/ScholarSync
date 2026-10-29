namespace ScholarSync.Models;

public class PasswordResetToken
{
    public int Id { get; set; }
    public int AdminUserId { get; set; }
    public AdminUser AdminUser { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
