using Swoosh.Api.Dtos;

namespace Swoosh.Api.Interfaces;

public interface INoteCardService
{
    Task<IEnumerable<NoteCardDto>> GetAllAsync(Guid userId);
    Task<NoteCardDto?> GetByIdAsync(Guid userId, Guid id);
    Task<NoteCardDto> CreateAsync(Guid userId, CreateNoteCardDto dto);
    Task<bool> UpdateAsync(Guid userId, Guid id, UpdateNoteCardDto dto);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
