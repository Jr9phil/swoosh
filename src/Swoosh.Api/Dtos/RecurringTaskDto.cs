using System.ComponentModel.DataAnnotations;

namespace Swoosh.Api.Dtos;

public class RecurringTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string RecurrenceType { get; set; } = "day";
    public int RecurrenceInterval { get; set; } = 1;
    public string? RecurrenceDate { get; set; }
    public string? RecurrenceTime { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
    public bool Pinned { get; set; }
    public int Rating { get; set; }
    public int? Icon { get; set; }
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
    [RegularExpression("^(day|week|month|year)$")]
    public string RecurrenceType { get; set; } = "day";

    [Range(1, 999)]
    public int RecurrenceInterval { get; set; } = 1;

    [RegularExpression("^\\d{4}-\\d{2}-\\d{2}$")]
    public string? RecurrenceDate { get; set; }

    [RegularExpression("^([01]\\d|2[0-3]):[0-5]\\d$")]
    public string? RecurrenceTime { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, 3)]
    public int Priority { get; set; } = 0;

    public bool Pinned { get; set; } = false;

    [Range(0, 5)]
    public int Rating { get; set; } = 0;

    public int? Icon { get; set; } = null;
}

public class UpdateRecurringTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [RegularExpression("^(day|week|month|year)$")]
    public string RecurrenceType { get; set; } = "day";

    [Range(1, 999)]
    public int RecurrenceInterval { get; set; } = 1;

    [RegularExpression("^\\d{4}-\\d{2}-\\d{2}$")]
    public string? RecurrenceDate { get; set; }

    [RegularExpression("^([01]\\d|2[0-3]):[0-5]\\d$")]
    public string? RecurrenceTime { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, 3)]
    public int Priority { get; set; } = 0;

    public bool Pinned { get; set; } = false;

    [Range(0, 5)]
    public int Rating { get; set; } = 0;

    public int? Icon { get; set; } = null;
}
