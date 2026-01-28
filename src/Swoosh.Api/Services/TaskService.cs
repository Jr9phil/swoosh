using Microsoft.EntityFrameworkCore;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Dtos;
using Swoosh.Api.Interfaces;
using Swoosh.Api.Security;

namespace Swoosh.Api.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _crypto;

    public TaskService(AppDbContext db, IEncryptionService crypto)
    {
        _db = db;
        _crypto = crypto;
    }

    public async Task<IEnumerable<TaskDto>> GetAllAsync(Guid userId)
    {
        return await _db.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = _crypto.Decrypt(t.EncryptedTitle, userId, t.KeyVersion),
                Notes = t.EncryptedNotes == null ? null : _crypto.Decrypt(t.EncryptedNotes, userId, t.KeyVersion),
                Deadline = t.Deadline,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TaskDto?> GetByIdAsync(Guid userId, Guid taskId)
    {
        return await _db.Tasks
            .Where(t => t.Id == taskId && t.UserId == userId)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = _crypto.Decrypt(t.EncryptedTitle, userId, t.KeyVersion),
                Notes = t.EncryptedNotes == null ? null : _crypto.Decrypt(t.EncryptedNotes, userId, t.KeyVersion),
                Deadline = t.Deadline,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<TaskDto> CreateAsync(Guid userId, CreateTaskDto dto)
    {
        var (encryptedTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId);
        
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EncryptedTitle = encryptedTitle,
            EncryptedNotes = dto.Notes == null ? null : _crypto.Encrypt(dto.Notes, userId).Ciphertext,
            KeyVersion = keyVersion,
            Deadline = dto.Deadline,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return new TaskDto
        {
            Id = task.Id,
            Title = dto.Title,
            Notes = dto.Notes,
            Deadline = task.Deadline,
            IsCompleted = task.IsCompleted,
            CreatedAt = task.CreatedAt
        };
    }

    public async Task<bool> UpdateAsync(Guid userId, Guid taskId, UpdateTaskDto dto)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
            return false;
        
        var (encryptedTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId);

        task.EncryptedTitle = encryptedTitle;
        task.EncryptedNotes = dto.Notes == null ? null : _crypto.Encrypt(dto.Notes, userId).Ciphertext;
        task.KeyVersion = keyVersion;
        task.Deadline = dto.Deadline;
        task.IsCompleted = dto.IsCompleted;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid taskId)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
            return false;

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return true;
    }
}
