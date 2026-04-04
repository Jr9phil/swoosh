namespace Swoosh.Api.Domain;

public class RecurringTask
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // RecurrenceType: "daily" | "interval" | "weekly" | "monthly" | "custom"
    public string EncryptedTitle { get; set; } = null!;
    public string EncryptedNotes { get; set; } = null!;
    public string EncryptedRecurrenceType { get; set; } = null!;
    public string? EncryptedRecurrenceInterval { get; set; } // number of days, for "interval" type
    public string EncryptedIsActive { get; set; } = null!;

    public int KeyVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}
