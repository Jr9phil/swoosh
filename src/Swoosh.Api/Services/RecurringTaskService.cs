using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Dtos;
using Swoosh.Api.Interfaces;
using Swoosh.Api.Security;

namespace Swoosh.Api.Services;

public class RecurringTaskService : IRecurringTaskService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _crypto;

    public RecurringTaskService(AppDbContext db, IEncryptionService crypto)
    {
        _db = db;
        _crypto = crypto;
    }

    private async Task<byte[]> GetUserSalt(Guid userId)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.EncryptionSalt)
            .SingleAsync();
    }

    private RecurringTaskDto Decrypt(RecurringTask r, Guid userId, byte[] salt)
    {
        return new RecurringTaskDto
        {
            Id = r.Id,
            Title = _crypto.Decrypt(r.EncryptedTitle, userId, r.KeyVersion, salt),
            Notes = _crypto.DecryptNullableString(r.EncryptedNotes, userId, r.KeyVersion, salt),
            RecurrenceType = _crypto.Decrypt(r.EncryptedRecurrenceType, userId, r.KeyVersion, salt),
            RecurrenceInterval = r.EncryptedRecurrenceInterval != null
                ? _crypto.DecryptNullableInt(r.EncryptedRecurrenceInterval, userId, r.KeyVersion, salt)
                : null,
            IsActive = _crypto.DecryptBool(r.EncryptedIsActive, userId, r.KeyVersion, salt),
            Priority = r.EncryptedPriority != null ? _crypto.DecryptInt(r.EncryptedPriority, userId, r.KeyVersion, salt) : 0,
            Pinned = r.EncryptedPinned != null && _crypto.DecryptBool(r.EncryptedPinned, userId, r.KeyVersion, salt),
            Rating = r.EncryptedRating != null ? _crypto.DecryptInt(r.EncryptedRating, userId, r.KeyVersion, salt) : 0,
            Icon = r.EncryptedIcon != null ? _crypto.DecryptNullableInt(r.EncryptedIcon, userId, r.KeyVersion, salt) : null,
            CreatedAt = r.CreatedAt,
            Modified = r.Modified
        };
    }

    public async Task<IEnumerable<RecurringTaskDto>> GetAllAsync(Guid userId)
    {
        var salt = await GetUserSalt(userId);
        var items = await _db.RecurringTasks
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = new List<RecurringTaskDto>();
        foreach (var r in items)
        {
            try { result.Add(Decrypt(r, userId, salt)); }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Failed to decrypt recurring task {r.Id}: {ex.Message}");
            }
        }
        return result;
    }

    public async Task<RecurringTaskDto?> GetByIdAsync(Guid userId, Guid id)
    {
        var salt = await GetUserSalt(userId);
        var r = await _db.RecurringTasks
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        return r == null ? null : Decrypt(r, userId, salt);
    }

    public async Task<RecurringTaskDto> CreateAsync(Guid userId, CreateRecurringTaskDto dto)
    {
        var salt = await GetUserSalt(userId);
        var (encTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);
        var now = DateTime.UtcNow;

        var entity = new RecurringTask
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EncryptedTitle = encTitle,
            EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext,
            EncryptedRecurrenceType = _crypto.Encrypt(dto.RecurrenceType, userId, salt).Ciphertext,
            EncryptedRecurrenceInterval = dto.RecurrenceInterval.HasValue
                ? _crypto.EncryptNullableInt(dto.RecurrenceInterval.Value, userId, salt).Ciphertext
                : null,
            EncryptedIsActive = _crypto.EncryptBool(dto.IsActive, userId, salt).Ciphertext,
            EncryptedPriority = _crypto.EncryptInt(dto.Priority, userId, salt).Ciphertext,
            EncryptedPinned = _crypto.EncryptBool(dto.Pinned, userId, salt).Ciphertext,
            EncryptedRating = _crypto.EncryptInt(dto.Rating, userId, salt).Ciphertext,
            EncryptedIcon = dto.Icon.HasValue ? _crypto.EncryptNullableInt(dto.Icon.Value, userId, salt).Ciphertext : null,
            KeyVersion = keyVersion,
            CreatedAt = now,
            Modified = now
        };

        _db.RecurringTasks.Add(entity);
        await _db.SaveChangesAsync();

        return new RecurringTaskDto
        {
            Id = entity.Id,
            Title = dto.Title,
            Notes = dto.Notes,
            RecurrenceType = dto.RecurrenceType,
            RecurrenceInterval = dto.RecurrenceInterval,
            IsActive = dto.IsActive,
            Priority = dto.Priority,
            Pinned = dto.Pinned,
            Rating = dto.Rating,
            Icon = dto.Icon,
            CreatedAt = now,
            Modified = now
        };
    }

    public async Task<bool> UpdateAsync(Guid userId, Guid id, UpdateRecurringTaskDto dto)
    {
        var entity = await _db.RecurringTasks
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (entity == null) return false;

        var salt = await GetUserSalt(userId);
        var (encTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);

        entity.EncryptedTitle = encTitle;
        entity.EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext;
        entity.EncryptedRecurrenceType = _crypto.Encrypt(dto.RecurrenceType, userId, salt).Ciphertext;
        entity.EncryptedRecurrenceInterval = dto.RecurrenceInterval.HasValue
            ? _crypto.EncryptNullableInt(dto.RecurrenceInterval.Value, userId, salt).Ciphertext
            : null;
        entity.EncryptedIsActive = _crypto.EncryptBool(dto.IsActive, userId, salt).Ciphertext;
        entity.EncryptedPriority = _crypto.EncryptInt(dto.Priority, userId, salt).Ciphertext;
        entity.EncryptedPinned = _crypto.EncryptBool(dto.Pinned, userId, salt).Ciphertext;
        entity.EncryptedRating = _crypto.EncryptInt(dto.Rating, userId, salt).Ciphertext;
        entity.EncryptedIcon = dto.Icon.HasValue ? _crypto.EncryptNullableInt(dto.Icon.Value, userId, salt).Ciphertext : null;
        entity.KeyVersion = keyVersion;
        entity.Modified = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var entity = await _db.RecurringTasks
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (entity == null) return false;

        _db.RecurringTasks.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
