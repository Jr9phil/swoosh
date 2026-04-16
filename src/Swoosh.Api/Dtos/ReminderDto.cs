using System.ComponentModel.DataAnnotations;

namespace Swoosh.Api.Dtos;

public class ReminderDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime RemindAt { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime Modified { get; set; }
}

public class CreateReminderDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public DateTime RemindAt { get; set; }

    public bool IsCompleted { get; set; } = false;
}

public class UpdateReminderDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public DateTime RemindAt { get; set; }

    public bool IsCompleted { get; set; }
}
