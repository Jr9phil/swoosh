using System.ComponentModel.DataAnnotations;

namespace Swoosh.Api.Dtos;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public DateTime? Deadline { get; set; }
    public bool IsCompleted { get; set; }
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
}

public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsCompleted { get; set; }
}