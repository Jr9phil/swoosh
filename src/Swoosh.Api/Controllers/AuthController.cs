using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Services;
using Swoosh.Api.Dtos;
using Swoosh.Api.Security;

namespace Swoosh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly IEncryptionService _encryption;

    public AuthController(AppDbContext db, AuthService auth, IEncryptionService encryption)
    {
        _db = db;
        _auth = auth;
        _encryption = encryption;
    }
    
    //Create User
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (_db.Users.Any(u => u.Email == dto.Email))
            return BadRequest("User already exists");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = _auth.HashPassword(dto.Password),
            EncryptionSalt = RandomNumberGenerator.GetBytes(16)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok();
    }
    
    //Login
    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        var user = _db.Users.SingleOrDefault(u => u.Email == dto.Email);
        if (user == null) return Unauthorized("Invalid credentials");

        if (!_auth.Verify(dto.Password, user.PasswordHash)) return Unauthorized("Invalid credentials");

        var token = _auth.GenerateToken(user);

        return Ok(new { token });
    }
    
    //Change Password 
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = UserContext.GetUserId(User);

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized();

        if (!_auth.Verify(dto.CurrentPassword, user.PasswordHash))
            return Unauthorized("Current password is incorrect");
        
        var tasks = await _db.Tasks
            .Where(t => t.UserId == userId)
            .ToListAsync();

        var oldSalt = user.EncryptionSalt;
        var newSalt = RandomNumberGenerator.GetBytes(16);
        
        foreach (var task in tasks)
        {
            var title = _encryption.Decrypt(
                task.EncryptedTitle,
                userId,
                task.KeyVersion,
                oldSalt);

            var (newTitle, newVersion1) =
                _encryption.Encrypt(title, userId, newSalt);

            task.EncryptedTitle = newTitle;
            task.KeyVersion = newVersion1;
            
            var notes = _encryption.DecryptNullableString(
                task.EncryptedNotes,
                userId,
                task.KeyVersion,
                oldSalt);

            var (newNotes, newVersion2) =
                _encryption.EncryptNullableString(
                    notes,
                    userId,
                    newSalt);

            task.EncryptedNotes = newNotes;
            task.KeyVersion = newVersion2;
            
            var deadline = _encryption.DecryptNullableDateTime(
                task.EncryptedDeadline,
                userId,
                task.KeyVersion,
                oldSalt);

            var (newDeadline, newVersion3) =
                _encryption.EncryptNullableDateTime(
                    deadline,
                    userId,
                    newSalt);

            task.EncryptedDeadline = newDeadline;
            task.KeyVersion = newVersion3;
            
            var priority = _encryption.DecryptInt(
                task.EncryptedPriority,
                userId,
                task.KeyVersion,
                oldSalt);

            var (newPriority, newVersion4) =
                _encryption.EncryptInt(
                    priority,
                    userId,
                    newSalt);

            task.EncryptedPriority = newPriority;
            task.KeyVersion = newVersion4;
            
            var pinned = _encryption.DecryptBool(
                task.EncryptedPinned,
                userId,
                task.KeyVersion,
                oldSalt);

            var (newPinned, newVersion5) =
                _encryption.EncryptBool(
                    pinned,
                    userId,
                    newSalt);

            task.EncryptedPinned = newPinned;
            task.KeyVersion = newVersion5;
            
            var completed = _encryption.DecryptNullableDateTime(
                task.EncryptedCompletedAt,
                userId,
                task.KeyVersion,
                oldSalt);

            var (newCompleted, newVersion6) =
                _encryption.EncryptNullableDateTime(
                    completed,
                    userId,
                    newSalt);

            task.EncryptedCompletedAt = newCompleted;
            task.KeyVersion = newVersion6;
        }
        
        user.EncryptionSalt = newSalt;
        
        user.PasswordHash = _auth.HashPassword(dto.NewPassword);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return StatusCode(500, "Password change failed");
        }
        
        return NoContent();
    }
}