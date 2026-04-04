using System.ComponentModel.DataAnnotations;

namespace Swoosh.Api.Dtos;

public class RecurringTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string RecurrenceType { get; set; } = "daily";
    public int? RecurrenceInterval { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime Modified { get; set; }
}

public class CreateRecurringTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [RegularExpression("^(daily|interval|weekly|monthly|custom)$")]
    public string RecurrenceType { get; set; } = "daily";

    [Range(1, 365)]
    public int? RecurrenceInterval { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateRecurringTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [RegularExpression("^(daily|interval|weekly|monthly|custom)$")]
    public string RecurrenceType { get; set; } = "daily";

    [Range(1, 365)]
    public int? RecurrenceInterval { get; set; }

    public bool IsActive { get; set; } = true;
}
