using Swoosh.Api.Dtos;

namespace Swoosh.Api.Interfaces;

public interface IRecurringTaskService
{
    Task<IEnumerable<RecurringTaskDto>> GetAllAsync(Guid userId);
    Task<RecurringTaskDto?> GetByIdAsync(Guid userId, Guid id);
    Task<RecurringTaskDto> CreateAsync(Guid userId, CreateRecurringTaskDto dto);
    Task<bool> UpdateAsync(Guid userId, Guid id, UpdateRecurringTaskDto dto);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
