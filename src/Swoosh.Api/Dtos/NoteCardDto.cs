using System.ComponentModel.DataAnnotations;

namespace Swoosh.Api.Dtos;

public class NoteCardDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime Modified { get; set; }
}

public class CreateNoteCardDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Body { get; set; }

    public int PositionX { get; set; } = 0;
    public int PositionY { get; set; } = 0;
}

public class UpdateNoteCardDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Body { get; set; }

    public int PositionX { get; set; } = 0;
    public int PositionY { get; set; } = 0;
}
