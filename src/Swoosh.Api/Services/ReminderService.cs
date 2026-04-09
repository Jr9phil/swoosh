using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Dtos;
using Swoosh.Api.Interfaces;
using Swoosh.Api.Security;

namespace Swoosh.Api.Services;

public class ReminderService : IReminderService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _crypto;

    public ReminderService(AppDbContext db, IEncryptionService crypto)
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

    private ReminderDto Decrypt(Reminder r, Guid userId, byte[] salt)
    {
        return new ReminderDto
        {
            Id = r.Id,
            Title = _crypto.Decrypt(r.EncryptedTitle, userId, r.KeyVersion, salt),
            Notes = _crypto.DecryptNullableString(r.EncryptedNotes, userId, r.KeyVersion, salt),
            RemindAt = _crypto.DecryptNullableDateTime(r.EncryptedRemindAt, userId, r.KeyVersion, salt) ?? DateTime.UtcNow,
            IsCompleted = _crypto.DecryptBool(r.EncryptedIsCompleted, userId, r.KeyVersion, salt),
            CreatedAt = r.CreatedAt,
            Modified = r.Modified
        };
    }

    public async Task<IEnumerable<ReminderDto>> GetAllAsync(Guid userId)
    {
        var salt = await GetUserSalt(userId);
        var items = await _db.Reminders
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = new List<ReminderDto>();
        foreach (var r in items)
        {
            try { result.Add(Decrypt(r, userId, salt)); }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Failed to decrypt reminder {r.Id}: {ex.Message}");
            }
        }
        return result;
    }

    public async Task<ReminderDto?> GetByIdAsync(Guid userId, Guid id)
    {
        var salt = await GetUserSalt(userId);
        var r = await _db.Reminders
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        return r == null ? null : Decrypt(r, userId, salt);
    }

    public async Task<ReminderDto> CreateAsync(Guid userId, CreateReminderDto dto)
    {
        var salt = await GetUserSalt(userId);
        var (encTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);
        var now = DateTime.UtcNow;

        var entity = new Reminder
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EncryptedTitle = encTitle,
            EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext,
            EncryptedRemindAt = _crypto.EncryptNullableDateTime(dto.RemindAt, userId, salt).Ciphertext,
            EncryptedIsCompleted = _crypto.EncryptBool(dto.IsCompleted, userId, salt).Ciphertext,
            KeyVersion = keyVersion,
            CreatedAt = now,
            Modified = now
        };

        _db.Reminders.Add(entity);
        await _db.SaveChangesAsync();

        return new ReminderDto
        {
            Id = entity.Id,
            Title = dto.Title,
            Notes = dto.Notes,
            RemindAt = dto.RemindAt,
            IsCompleted = dto.IsCompleted,
            CreatedAt = now,
            Modified = now
        };
    }

    public async Task<bool> UpdateAsync(Guid userId, Guid id, UpdateReminderDto dto)
    {
        var entity = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (entity == null) return false;

        var salt = await GetUserSalt(userId);
        var (encTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);

        entity.EncryptedTitle = encTitle;
        entity.EncryptedNotes = _crypto.EncryptNullableString(dto.Notes, userId, salt).Ciphertext;
        entity.EncryptedRemindAt = _crypto.EncryptNullableDateTime(dto.RemindAt, userId, salt).Ciphertext;
        entity.EncryptedIsCompleted = _crypto.EncryptBool(dto.IsCompleted, userId, salt).Ciphertext;
        entity.KeyVersion = keyVersion;
        entity.Modified = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var entity = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (entity == null) return false;

        _db.Reminders.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
