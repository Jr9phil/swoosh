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
    public string EncryptedPriority { get; set; } = null!;   // 0–3
    public string EncryptedPinned { get; set; } = null!;
    public string? EncryptedRating { get; set; }             // 0–5, nullable
    public string? EncryptedIcon { get; set; }               // nullable int

    public int KeyVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}
