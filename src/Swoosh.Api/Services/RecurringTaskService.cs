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

    private static bool OccursOnDay(string type, int interval, DateOnly start, DateOnly day)
    {
        if (day < start) return false;
        if (day == start) return true;
        return type switch
        {
            "day"   => (day.DayNumber - start.DayNumber) % interval == 0,
            "week"  => (day.DayNumber - start.DayNumber) % (7 * interval) == 0,
            "month" => day.Day == start.Day &&
                       ((day.Year - start.Year) * 12 + (day.Month - start.Month)) % interval == 0,
            "year"  => day.Month == start.Month && day.Day == start.Day &&
                       (day.Year - start.Year) % interval == 0,
            _       => false
        };
    }

    // Promotes interval+type to a coarser unit when the division is exact.
    // e.g. 7 days → 1 week, 14 days → 2 weeks, 52 weeks → 1 year, 12 months → 1 year
    private static (string Type, int Interval) Normalize(string type, int interval)
    {
        return type switch
        {
            "day" when interval % 365 == 0 => ("year",  interval / 365),
            "day" when interval % 7   == 0 => ("week",  interval / 7),
            "week"  when interval % 52 == 0 => ("year",  interval / 52),
            "month" when interval % 12 == 0 => ("year",  interval / 12),
            _ => (type, interval)
        };
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
                ? (_crypto.DecryptNullableInt(r.EncryptedRecurrenceInterval, userId, r.KeyVersion, salt) ?? 1)
                : 1,
            RecurrenceDate = r.EncryptedDate != null
                ? _crypto.DecryptNullableString(r.EncryptedDate, userId, r.KeyVersion, salt)
                : null,
            RecurrenceTime = r.EncryptedTime != null
                ? _crypto.DecryptNullableString(r.EncryptedTime, userId, r.KeyVersion, salt)
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
        (dto.RecurrenceType, dto.RecurrenceInterval) = Normalize(dto.RecurrenceType, dto.RecurrenceInterval);

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
            EncryptedRecurrenceInterval = _crypto.EncryptInt(dto.RecurrenceInterval, userId, salt).Ciphertext,
            EncryptedDate = dto.RecurrenceDate != null
                ? _crypto.EncryptNullableString(dto.RecurrenceDate, userId, salt).Ciphertext
                : null,
            EncryptedTime = dto.RecurrenceTime != null
                ? _crypto.EncryptNullableString(dto.RecurrenceTime, userId, salt).Ciphertext
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

        // Spawn a task immediately if today is an occurrence day.
        if (dto.IsActive)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = dto.RecurrenceDate != null && DateOnly.TryParse(dto.RecurrenceDate, out var d) ? d : today;
            if (OccursOnDay(dto.RecurrenceType, dto.RecurrenceInterval, start, today))
            {
                DateTime deadline;
                if (dto.RecurrenceTime != null && TimeOnly.TryParse(dto.RecurrenceTime, out var t))
                    deadline = new DateTime(start.Year, start.Month, start.Day, t.Hour, t.Minute, 0, DateTimeKind.Unspecified);
                else
                    deadline = new DateTime(start.Year, start.Month, start.Day, 23, 59, 0, DateTimeKind.Unspecified);

                var (encSpawnTitle, spawnKeyVer) = _crypto.Encrypt(dto.Title, userId, salt);
                _db.Tasks.Add(new Domain.TaskItem
                {
                    Id                   = Guid.NewGuid(),
                    UserId               = userId,
                    RecurringTaskId      = entity.Id,
                    EncryptedTitle       = encSpawnTitle,
                    EncryptedNotes       = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext,
                    EncryptedDeadline    = _crypto.EncryptNullableDateTime(deadline, userId, salt).Ciphertext,
                    EncryptedCompletedAt = _crypto.EncryptNullableDateTime(null, userId, salt).Ciphertext,
                    EncryptedPinned      = _crypto.EncryptBool(dto.Pinned, userId, salt).Ciphertext,
                    EncryptedPriority    = _crypto.EncryptInt(dto.Priority, userId, salt).Ciphertext,
                    EncryptedRating      = _crypto.EncryptInt(0, userId, salt).Ciphertext,
                    EncryptedIcon        = dto.Icon.HasValue ? _crypto.EncryptNullableInt(dto.Icon.Value, userId, salt).Ciphertext : null,
                    KeyVersion           = spawnKeyVer,
                    CreatedAt            = now,
                    Modified             = now,
                });

                entity.EncryptedLastSpawnedDate = _crypto.EncryptNullableString(today.ToString("yyyy-MM-dd"), userId, salt).Ciphertext;
                await _db.SaveChangesAsync();
            }
        }

        return new RecurringTaskDto
        {
            Id = entity.Id,
            Title = dto.Title,
            Notes = dto.Notes,
            RecurrenceType = dto.RecurrenceType,
            RecurrenceInterval = dto.RecurrenceInterval,
            RecurrenceDate = dto.RecurrenceDate,
            RecurrenceTime = dto.RecurrenceTime,
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

        (dto.RecurrenceType, dto.RecurrenceInterval) = Normalize(dto.RecurrenceType, dto.RecurrenceInterval);

        var salt = await GetUserSalt(userId);
        var (encTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);

        entity.EncryptedTitle = encTitle;
        entity.EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext;
        entity.EncryptedRecurrenceType = _crypto.Encrypt(dto.RecurrenceType, userId, salt).Ciphertext;
        entity.EncryptedRecurrenceInterval = _crypto.EncryptInt(dto.RecurrenceInterval, userId, salt).Ciphertext;
        entity.EncryptedDate = dto.RecurrenceDate != null
            ? _crypto.EncryptNullableString(dto.RecurrenceDate, userId, salt).Ciphertext
            : null;
        entity.EncryptedTime = dto.RecurrenceTime != null
            ? _crypto.EncryptNullableString(dto.RecurrenceTime, userId, salt).Ciphertext
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
