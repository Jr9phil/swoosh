namespace Swoosh.Api.Domain;

public class Reminder
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string EncryptedTitle { get; set; } = null!;
    public string EncryptedNotes { get; set; } = null!;
    public string EncryptedRemindAt { get; set; } = null!;
    public string EncryptedIsCompleted { get; set; } = null!;

    public int KeyVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}
