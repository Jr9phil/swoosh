using Swoosh.Api.Dtos;

namespace Swoosh.Api.Interfaces;

public interface IReminderService
{
    Task<IEnumerable<ReminderDto>> GetAllAsync(Guid userId);
    Task<ReminderDto?> GetByIdAsync(Guid userId, Guid id);
    Task<ReminderDto> CreateAsync(Guid userId, CreateReminderDto dto);
    Task<bool> UpdateAsync(Guid userId, Guid id, UpdateReminderDto dto);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
