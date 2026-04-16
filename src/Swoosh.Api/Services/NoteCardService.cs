using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Dtos;
using Swoosh.Api.Interfaces;
using Swoosh.Api.Security;

namespace Swoosh.Api.Services;

public class NoteCardService : INoteCardService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _crypto;

    public NoteCardService(AppDbContext db, IEncryptionService crypto)
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

    private NoteCardDto Decrypt(NoteCard n, Guid userId, byte[] salt)
    {
        return new NoteCardDto
        {
            Id = n.Id,
            Title = _crypto.Decrypt(n.EncryptedTitle, userId, n.KeyVersion, salt),
            Body = _crypto.DecryptNullableString(n.EncryptedBody, userId, n.KeyVersion, salt),
            PositionX = _crypto.DecryptInt(n.EncryptedPositionX, userId, n.KeyVersion, salt),
            PositionY = _crypto.DecryptInt(n.EncryptedPositionY, userId, n.KeyVersion, salt),
            CreatedAt = n.CreatedAt,
            Modified = n.Modified
        };
    }

    public async Task<IEnumerable<NoteCardDto>> GetAllAsync(Guid userId)
    {
        var salt = await GetUserSalt(userId);
        var items = await _db.NoteCards
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var result = new List<NoteCardDto>();
        foreach (var n in items)
        {
            try { result.Add(Decrypt(n, userId, salt)); }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Failed to decrypt note card {n.Id}: {ex.Message}");
            }
        }
        return result;
    }

    public async Task<NoteCardDto?> GetByIdAsync(Guid userId, Guid id)
    {
        var salt = await GetUserSalt(userId);
        var n = await _db.NoteCards
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        return n == null ? null : Decrypt(n, userId, salt);
    }

    public async Task<NoteCardDto> CreateAsync(Guid userId, CreateNoteCardDto dto)
    {
        var salt = await GetUserSalt(userId);
        var (encTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);
        var now = DateTime.UtcNow;

        var entity = new NoteCard
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EncryptedTitle = encTitle,
            EncryptedBody = _crypto.EncryptNullableString(dto.Body, userId, salt).Ciphertext,
            EncryptedPositionX = _crypto.EncryptInt(dto.PositionX, userId, salt).Ciphertext,
            EncryptedPositionY = _crypto.EncryptInt(dto.PositionY, userId, salt).Ciphertext,
            KeyVersion = keyVersion,
            CreatedAt = now,
            Modified = now
        };

        _db.NoteCards.Add(entity);
        await _db.SaveChangesAsync();

        return new NoteCardDto
        {
            Id = entity.Id,
            Title = dto.Title,
            Body = dto.Body,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            CreatedAt = now,
            Modified = now
        };
    }

    public async Task<bool> UpdateAsync(Guid userId, Guid id, UpdateNoteCardDto dto)
    {
        var entity = await _db.NoteCards
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (entity == null) return false;

        var salt = await GetUserSalt(userId);
        var (encTitle, keyVersion) = _crypto.Encrypt(dto.Title, userId, salt);

        entity.EncryptedTitle = encTitle;
        entity.EncryptedBody = _crypto.EncryptNullableString(dto.Body, userId, salt).Ciphertext;
        entity.EncryptedPositionX = _crypto.EncryptInt(dto.PositionX, userId, salt).Ciphertext;
        entity.EncryptedPositionY = _crypto.EncryptInt(dto.PositionY, userId, salt).Ciphertext;
        entity.KeyVersion = keyVersion;
        entity.Modified = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var entity = await _db.NoteCards
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (entity == null) return false;

        _db.NoteCards.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
