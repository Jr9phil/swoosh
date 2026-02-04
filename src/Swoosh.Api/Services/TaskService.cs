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

    private async Task<byte[]> GetUserSalt(Guid userId)
    {
        try
        {
            return await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.EncryptionSalt)
                .SingleAsync();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new Exception("Failed while retrieving user salt", e);
        }
    }

    public async Task<IEnumerable<TaskDto>> GetAllAsync(Guid userId)
    {
        var salt = await GetUserSalt(userId);
        return await _db.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = _crypto.Decrypt(t.EncryptedTitle, userId, t.KeyVersion, salt),
                Notes = _crypto.DecryptNullableString(t.EncryptedNotes, userId, t.KeyVersion, salt),
                Deadline = _crypto.DecryptNullableDateTime(t.EncryptedDeadline, userId, t.KeyVersion, salt),
                Completed = _crypto.DecryptNullableDateTime(t.EncryptedCompletedAt, userId, t.KeyVersion, salt),
                Pinned = _crypto.DecryptBool(t.EncryptedPinned, userId, t.KeyVersion, salt),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TaskDto?> GetByIdAsync(Guid userId, Guid taskId)
    {
        var salt = await GetUserSalt(userId);
        return await _db.Tasks
            .Where(t => t.Id == taskId && t.UserId == userId)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = _crypto.Decrypt(t.EncryptedTitle, userId, t.KeyVersion, salt),
                Notes = _crypto.DecryptNullableString(t.EncryptedNotes, userId, t.KeyVersion, salt),
                Deadline = _crypto.DecryptNullableDateTime(t.EncryptedDeadline, userId, t.KeyVersion, salt),
                Completed = _crypto.DecryptNullableDateTime(t.EncryptedCompletedAt, userId, t.KeyVersion, salt),
                Pinned = _crypto.DecryptBool(t.EncryptedPinned, userId, t.KeyVersion, salt),
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<TaskDto> CreateAsync(Guid userId, CreateTaskDto dto)
    {
        var salt = await GetUserSalt(userId);
        var (encryptedTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);
        
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EncryptedTitle = encryptedTitle,
            EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext,
            EncryptedCompletedAt = _crypto.EncryptNullableDateTime(dto.Completed, userId, salt).Ciphertext,
            EncryptedDeadline = _crypto.EncryptNullableDateTime(dto.Deadline, userId, salt).Ciphertext,
            EncryptedPinned = _crypto.EncryptBool(dto.Pinned, userId, salt).Ciphertext,
            KeyVersion = keyVersion,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return new TaskDto
        {
            Id = task.Id,
            Title = dto.Title,
            Notes = dto.Notes,
            Completed = dto.Completed,
            Deadline = dto.Deadline,
            Pinned = dto.Pinned,
            CreatedAt = task.CreatedAt
        };
    }

    public async Task<bool> UpdateAsync(Guid userId, Guid taskId, UpdateTaskDto dto)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
            return false;
        
        var salt = await GetUserSalt(userId);
        var (encryptedTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);

        task.EncryptedTitle = encryptedTitle;
        task.EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext;
        task.EncryptedCompletedAt = _crypto.EncryptNullableDateTime(dto.Completed, userId, salt).Ciphertext;
        task.EncryptedDeadline = _crypto.EncryptNullableDateTime(dto.Deadline, userId, salt).Ciphertext;
        task.EncryptedPinned = _crypto.EncryptBool(dto.Pinned, userId, salt).Ciphertext;
        task.KeyVersion = keyVersion;

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
