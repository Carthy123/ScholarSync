namespace ScholarSync.Models;

public class AnnouncementAcknowledgement
{
    public int Id { get; set; }
    public int AnnouncementId { get; set; }
    public SchoolAnnouncement Announcement { get; set; } = null!;
    public string Username { get; set; } = string.Empty;
    public DateTime AcknowledgedAt { get; set; } = DateTime.UtcNow;
}
