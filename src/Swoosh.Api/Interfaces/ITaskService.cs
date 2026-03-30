using Swoosh.Api.Dtos;

namespace Swoosh.Api.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskDto>> GetAllAsync(Guid userId);
    Task<TaskDto?> GetByIdAsync(Guid userId, Guid taskId);
    Task<TaskDto> CreateAsync(Guid userId, CreateTaskDto dto);
    Task<SubtaskDto> CreateSubtaskAsync(Guid userId, Guid parentTaskId, CreateSubtaskDto dto);
    Task<bool> UpdateAsync(Guid userId, Guid taskId, UpdateTaskDto dto);
    Task<bool> AttachToParentAsync(Guid userId, Guid childId, Guid parentId);
    Task<bool> DeleteAsync(Guid userId, Guid taskId);
}