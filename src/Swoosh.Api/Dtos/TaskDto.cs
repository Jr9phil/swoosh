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
    public Guid UserId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; } = null;
    public bool IsCompleted { get; set; } = false;
}