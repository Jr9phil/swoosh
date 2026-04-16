namespace Swoosh.Api.Domain;

public class NoteCard
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string EncryptedTitle { get; set; } = null!;
    public string EncryptedBody { get; set; } = null!;
    public string EncryptedPositionX { get; set; } = null!;
    public string EncryptedPositionY { get; set; } = null!;

    public int KeyVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}
