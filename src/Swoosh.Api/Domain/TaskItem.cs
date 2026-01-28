namespace Swoosh.Api.Domain;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string EncryptedTitle { get; set; } = null!;
    public string? EncryptedNotes { get; set; }
    
    public int KeyVersion { get; set; }
    
    public DateTime? Deadline { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}