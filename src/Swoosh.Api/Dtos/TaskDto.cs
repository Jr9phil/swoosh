using System.ComponentModel.DataAnnotations;

namespace Swoosh.Api.Dtos;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    
    public DateTime? Completed { get; set; }
    public DateTime? Deadline { get; set; }
    public bool Pinned { get; set; }
    public int Priority { get; set; }

    public DateTime CreatedAt { get; set; }
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
}