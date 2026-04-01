using System.ComponentModel.DataAnnotations;

namespace Swoosh.Api.Dtos;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public DateTime? Completed { get; set; }
    public DateTime? Deadline { get; set; }
    public bool Pinned { get; set; }
    public int Priority { get; set; }
    public int Rating { get; set; }
    public int? Icon { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime Modified { get; set; }
}

public class CreateTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    public DateTime? Deadline { get; set; } = null;
    public DateTime? Completed { get; set; } = null;
    public bool Pinned { get; set; } = false;
    [Range(0, 3)]
    public int Priority { get; set; } = 0;

    [Range(0, 5)]
    public int Rating { get; set; } = 0;

    public int? Icon { get; set; } = null;
}

public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? Deadline { get; set; }

    public DateTime? Completed { get; set; }

    public bool Pinned { get; set; }
    [Range(0, 3)]
    public int Priority { get; set; } = 0;

    [Range(0, 5)]
    public int Rating { get; set; } = 0;

    public int? Icon { get; set; } = null;

    // When provided (e.g. drag-to-reorder across priority groups), the backend uses this
    // value instead of DateTime.UtcNow so the sort position is preserved exactly.
    public DateTime? Modified { get; set; } = null;
}

public class ReorderTaskDto
{
    public DateTime Modified { get; set; }
}

public class SubtaskDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime? Completed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime Modified { get; set; }
}

public class CreateSubtaskDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }
    public DateTime? Deadline { get; set; } = null;
    public DateTime? Completed { get; set; }
}

public class DetachFromParentDto
{
    [Range(0, 3)]
    public int Priority { get; set; } = 0;
    public DateTime Modified { get; set; }
}