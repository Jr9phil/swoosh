using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Dtos;
using Swoosh.Api.Interfaces;
using Swoosh.Api.Security;

namespace Swoosh.Api.Services;

/// Service for managing user tasks, handling encryption/decryption of task data before storage/retrieval.
public class TaskService : ITaskService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _crypto;

    public TaskService(AppDbContext db, IEncryptionService crypto)
    {
        _db = db;
        _crypto = crypto;
    }

    /// Retrieves the encryption salt for a specific user.
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

    /// Fetches all tasks for a specific user and decrypts their fields.
    public async Task<IEnumerable<TaskDto>> GetAllAsync(Guid userId)
    {
        var salt = await GetUserSalt(userId);
        
        var tasks = await _db.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var result = new List<TaskDto>();

        foreach (var t in tasks)
        {
            try
            {
                result.Add(new TaskDto
                {
                    Id = t.Id,
                    ParentId = t.ParentId,
                    RecurringTaskId = t.RecurringTaskId,
                    Title = _crypto.Decrypt(t.EncryptedTitle, userId, t.KeyVersion, salt),
                    Notes = _crypto.DecryptNullableString(t.EncryptedNotes, userId, t.KeyVersion, salt),
                    Deadline = _crypto.DecryptNullableDateTime(t.EncryptedDeadline, userId, t.KeyVersion, salt),
                    Completed = _crypto.DecryptNullableDateTime(t.EncryptedCompletedAt, userId, t.KeyVersion, salt),
                    Pinned = _crypto.DecryptBool(t.EncryptedPinned, userId, t.KeyVersion, salt),
                    Priority = _crypto.DecryptInt(t.EncryptedPriority, userId, t.KeyVersion, salt),
                    Rating = t.EncryptedRating != null ? _crypto.DecryptInt(t.EncryptedRating, userId, t.KeyVersion, salt) : 0,
                    Icon = t.EncryptedIcon != null ? _crypto.DecryptNullableInt(t.EncryptedIcon, userId, t.KeyVersion, salt) : null,
                    TimerDuration = t.EncryptedTimerDuration != null ? _crypto.DecryptNullableInt(t.EncryptedTimerDuration, userId, t.KeyVersion, salt) : null,
                    CreatedAt = t.CreatedAt,
                    Modified = t.Modified
                });
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Failed to decrypt task {t.Id}: {ex.Message}");
            }
        }

        // Exclude completed tasks older than 30 days (those belong in the archive).
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        result = result
            .Where(t => !(t.Completed.HasValue && t.Completed.Value < thirtyDaysAgo))
            .ToList();

        // Subtask priority is inferred from the parent at read time.
        var parentPriorities = result
            .Where(t => t.ParentId == null)
            .ToDictionary(t => t.Id, t => t.Priority);

        foreach (var dto in result)
        {
            if (dto.ParentId.HasValue && parentPriorities.TryGetValue(dto.ParentId.Value, out var parentPriority))
                dto.Priority = parentPriority;
        }

        return result;
    }

    /// Fetches completed tasks older than 30 days for the archive view.
    public async Task<IEnumerable<TaskDto>> GetArchivedAsync(Guid userId)
    {
        var salt = await GetUserSalt(userId);

        var tasks = await _db.Tasks
            .Where(t => t.UserId == userId && t.ParentId == null)
            .OrderByDescending(t => t.Modified)
            .ToListAsync();

        var result = new List<TaskDto>();
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        foreach (var t in tasks)
        {
            try
            {
                var completed = _crypto.DecryptNullableDateTime(t.EncryptedCompletedAt, userId, t.KeyVersion, salt);
                if (!completed.HasValue || completed.Value >= thirtyDaysAgo) continue;

                result.Add(new TaskDto
                {
                    Id = t.Id,
                    ParentId = t.ParentId,
                    RecurringTaskId = t.RecurringTaskId,
                    Title = _crypto.Decrypt(t.EncryptedTitle, userId, t.KeyVersion, salt),
                    Notes = _crypto.DecryptNullableString(t.EncryptedNotes, userId, t.KeyVersion, salt),
                    Deadline = _crypto.DecryptNullableDateTime(t.EncryptedDeadline, userId, t.KeyVersion, salt),
                    Completed = completed,
                    Pinned = _crypto.DecryptBool(t.EncryptedPinned, userId, t.KeyVersion, salt),
                    Priority = _crypto.DecryptInt(t.EncryptedPriority, userId, t.KeyVersion, salt),
                    Rating = t.EncryptedRating != null ? _crypto.DecryptInt(t.EncryptedRating, userId, t.KeyVersion, salt) : 0,
                    Icon = t.EncryptedIcon != null ? _crypto.DecryptNullableInt(t.EncryptedIcon, userId, t.KeyVersion, salt) : null,
                    TimerDuration = t.EncryptedTimerDuration != null ? _crypto.DecryptNullableInt(t.EncryptedTimerDuration, userId, t.KeyVersion, salt) : null,
                    CreatedAt = t.CreatedAt,
                    Modified = t.Modified
                });
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Failed to decrypt archived task {t.Id}: {ex.Message}");
            }
        }

        return result;
    }

    /// Retrieves a specific task by ID for a user and decrypts its fields.
    public async Task<TaskDto?> GetByIdAsync(Guid userId, Guid taskId)
    {
        var salt = await GetUserSalt(userId);
        return await _db.Tasks
            .Where(t => t.Id == taskId && t.UserId == userId)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                ParentId = t.ParentId,
                    RecurringTaskId = t.RecurringTaskId,
                Title = _crypto.Decrypt(t.EncryptedTitle, userId, t.KeyVersion, salt),
                Notes = _crypto.DecryptNullableString(t.EncryptedNotes, userId, t.KeyVersion, salt),
                Deadline = _crypto.DecryptNullableDateTime(t.EncryptedDeadline, userId, t.KeyVersion, salt),
                Completed = _crypto.DecryptNullableDateTime(t.EncryptedCompletedAt, userId, t.KeyVersion, salt),
                Pinned = _crypto.DecryptBool(t.EncryptedPinned, userId, t.KeyVersion, salt),
                Priority = _crypto.DecryptInt(t.EncryptedPriority, userId, t.KeyVersion, salt),
                Rating = t.EncryptedRating != null ? _crypto.DecryptInt(t.EncryptedRating, userId, t.KeyVersion, salt) : 0,
                Icon = t.EncryptedIcon != null ? _crypto.DecryptNullableInt(t.EncryptedIcon, userId, t.KeyVersion, salt) : null,
                TimerDuration = t.EncryptedTimerDuration != null ? _crypto.DecryptNullableInt(t.EncryptedTimerDuration, userId, t.KeyVersion, salt) : null,
                CreatedAt = t.CreatedAt,
                Modified = t.Modified
            })
            .FirstOrDefaultAsync();
    }
    
    //Get a subtask by ID
    public async Task<SubtaskDto?> GetByIdAsync(Guid userId, Guid taskId, Guid? parentId)
    {
        if (parentId == null) return null;
        
        var salt = await GetUserSalt(userId);
        return await _db.Tasks
            .Where(t => t.Id == taskId && t.UserId == userId && t.ParentId == parentId)
            .Select(t => new SubtaskDto
            {
                Id = t.Id,
                ParentId = t.ParentId,
                Title = _crypto.Decrypt(t.EncryptedTitle, userId, t.KeyVersion, salt),
                Notes = _crypto.DecryptNullableString(t.EncryptedNotes, userId, t.KeyVersion, salt),
                Deadline = _crypto.DecryptNullableDateTime(t.EncryptedDeadline, userId, t.KeyVersion, salt),
                Completed = _crypto.DecryptNullableDateTime(t.EncryptedCompletedAt, userId, t.KeyVersion, salt),
                CreatedAt = t.CreatedAt,
                Modified = t.Modified
            })
            .FirstOrDefaultAsync();
    }

    /// Creates a new task, encrypting all sensitive fields before saving to the database.
    public async Task<TaskDto> CreateAsync(Guid userId, CreateTaskDto dto)
    {
        var salt = await GetUserSalt(userId);
        var (encryptedTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);
        var encryptedPriority = _crypto.EncryptInt(
            Math.Clamp(dto.Priority, 0, 3),
            userId,
            salt
        );
        var now = DateTime.UtcNow;

        
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EncryptedTitle = encryptedTitle,
            EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext,
            EncryptedCompletedAt = _crypto.EncryptNullableDateTime(dto.Completed, userId, salt).Ciphertext,
            EncryptedDeadline = _crypto.EncryptNullableDateTime(dto.Deadline, userId, salt).Ciphertext,
            EncryptedPinned = _crypto.EncryptBool(dto.Pinned, userId, salt).Ciphertext,
            EncryptedPriority = encryptedPriority.Ciphertext,
            EncryptedRating = _crypto.EncryptInt(Math.Clamp(dto.Rating, 0, 5), userId, salt).Ciphertext,
            EncryptedIcon = dto.Icon.HasValue ? _crypto.EncryptNullableInt(dto.Icon.Value, userId, salt).Ciphertext : null,
            EncryptedTimerDuration = dto.TimerDuration.HasValue ? _crypto.EncryptNullableInt(dto.TimerDuration.Value, userId, salt).Ciphertext : null,
            KeyVersion = keyVersion,
            CreatedAt = now,
            Modified = now
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
            Priority = dto.Priority,
            Rating = dto.Rating,
            Icon = dto.Icon,
            TimerDuration = dto.TimerDuration,
            CreatedAt = task.CreatedAt,
            Modified = task.Modified
        };
    }
    
    // Creates a new subtask
    public async Task<SubtaskDto> CreateSubtaskAsync(Guid userId, Guid parentTaskId, CreateSubtaskDto dto)
    {
        var salt = await GetUserSalt(userId);
        var (encryptedTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);
        var now = DateTime.UtcNow;

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ParentId = parentTaskId,
            EncryptedTitle = encryptedTitle,
            EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext,
            EncryptedCompletedAt = _crypto.EncryptNullableDateTime(dto.Completed, userId, salt).Ciphertext,
            EncryptedDeadline = _crypto.EncryptNullableDateTime(dto.Deadline, userId, salt).Ciphertext,
            EncryptedPinned = _crypto.EncryptBool(false, userId, salt).Ciphertext,
            EncryptedPriority = _crypto.EncryptInt(0, userId, salt).Ciphertext,
            EncryptedRating = _crypto.EncryptInt(0, userId, salt).Ciphertext,
            EncryptedIcon = null,
            EncryptedTimerDuration = dto.TimerDuration.HasValue ? _crypto.EncryptNullableInt(dto.TimerDuration.Value, userId, salt).Ciphertext : null,
            KeyVersion = keyVersion,
            CreatedAt = now,
            Modified = now
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return new SubtaskDto
        {
            Id = task.Id,
            ParentId = parentTaskId,
            Title = dto.Title,
            Notes = dto.Notes,
            Completed = dto.Completed,
            Deadline = dto.Deadline,
            TimerDuration = dto.TimerDuration,
            CreatedAt = task.CreatedAt,
            Modified = task.Modified
        };
    }

    /// Updates an existing task, re-encrypting updated fields.
    public async Task<bool> UpdateAsync(Guid userId, Guid taskId, UpdateTaskDto dto)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
            return false;

        var salt = await GetUserSalt(userId);

        // Block completing a parent task when it has incomplete subtasks with deadlines.
        if (dto.Completed.HasValue && task.ParentId == null)
        {
            var subtasks = await _db.Tasks
                .Where(t => t.ParentId == taskId && t.UserId == userId)
                .ToListAsync();

            foreach (var subtask in subtasks)
            {
                var deadline = _crypto.DecryptNullableDateTime(subtask.EncryptedDeadline, userId, subtask.KeyVersion, salt);
                var completed = _crypto.DecryptNullableDateTime(subtask.EncryptedCompletedAt, userId, subtask.KeyVersion, salt);
                if (deadline.HasValue && !completed.HasValue)
                    throw new InvalidOperationException("Cannot complete a task while subtasks with deadlines are still incomplete.");
            }
        }
        var (encryptedTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);

        task.EncryptedTitle = encryptedTitle;
        task.EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext;
        task.EncryptedCompletedAt = _crypto.EncryptNullableDateTime(dto.Completed, userId, salt).Ciphertext;
        task.EncryptedDeadline = _crypto.EncryptNullableDateTime(dto.Deadline, userId, salt).Ciphertext;
        task.EncryptedPinned = _crypto.EncryptBool(dto.Pinned, userId, salt).Ciphertext;
        task.EncryptedPriority = _crypto.EncryptInt(Math.Clamp(dto.Priority, 0, 3), userId, salt).Ciphertext;
        task.EncryptedRating = _crypto.EncryptInt(Math.Clamp(dto.Rating, 0, 5), userId, salt).Ciphertext;
        task.EncryptedIcon = dto.Icon.HasValue ? _crypto.EncryptNullableInt(dto.Icon.Value, userId, salt).Ciphertext : null;
        task.EncryptedTimerDuration = dto.TimerDuration.HasValue ? _crypto.EncryptNullableInt(dto.TimerDuration.Value, userId, salt).Ciphertext : null;
        task.KeyVersion = keyVersion;
        task.Modified = dto.Modified ?? DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    /// Updates only the Modified timestamp of a task for manual reordering.
    /// No re-encryption is needed since no encrypted fields change.
    public async Task<bool> ReorderAsync(Guid userId, Guid taskId, DateTime modified)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
            return false;

        task.Modified = modified;
        await _db.SaveChangesAsync();
        return true;
    }

    /// Converts a top-level task into a subtask of another task.
    public async Task<bool> AttachToParentAsync(Guid userId, Guid childId, Guid parentId)
    {
        var child = await _db.Tasks.FirstOrDefaultAsync(
            t => t.Id == childId && t.UserId == userId && t.ParentId == null);
        if (child == null) return false;

        var parent = await _db.Tasks.FirstOrDefaultAsync(
            t => t.Id == parentId && t.UserId == userId && t.ParentId == null);
        if (parent == null) return false;

        var hasSubtasks = await _db.Tasks.AnyAsync(t => t.ParentId == childId && t.UserId == userId);
        if (hasSubtasks) return false;

        var salt = await GetUserSalt(userId);
        child.ParentId = parentId;
        child.EncryptedPinned = _crypto.EncryptBool(false, userId, salt).Ciphertext;
        child.EncryptedRating = _crypto.EncryptInt(0, userId, salt).Ciphertext;
        child.EncryptedIcon = null;
        child.Modified = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    /// Promotes a subtask to a top-level task, assigning it a target priority and sort position.
    public async Task<bool> DetachFromParentAsync(Guid userId, Guid taskId, int targetPriority, DateTime modified)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(
            t => t.Id == taskId && t.UserId == userId && t.ParentId != null);
        if (task == null) return false;

        var salt = await GetUserSalt(userId);
        task.ParentId = null;
        task.EncryptedPriority = _crypto.EncryptInt(Math.Clamp(targetPriority, 0, 3), userId, salt).Ciphertext;
        task.Modified = modified;

        await _db.SaveChangesAsync();
        return true;
    }

    /// Deletes a task and its subtasks from the database.
    public async Task<bool> DeleteAsync(Guid userId, Guid taskId)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
            return false;

        var subtasks = await _db.Tasks
            .Where(t => t.ParentId == taskId && t.UserId == userId)
            .ToListAsync();

        _db.Tasks.RemoveRange(subtasks);
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return true;
    }
}
