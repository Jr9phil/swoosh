namespace Swoosh.Api.Domain;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string EncryptedTitle { get; set; } = null!;
    public string EncryptedNotes { get; set; } = null!;
    public string EncryptedCompletedAt { get; set; } = null!;
    public string EncryptedDeadline { get; set; } = null!;
    public string EncryptedPinned { get; set; } = null!;
    public string EncryptedPriority { get; set; } = null!;
    
    public int KeyVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}