using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

    public AuthController(AppDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }
    
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
    
    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        var user = _db.Users.SingleOrDefault(u => u.Email == dto.Email);
        if (user == null) return Unauthorized("Invalid credentials");

        if (!_auth.Verify(dto.Password, user.PasswordHash)) return Unauthorized("Invalid credentials");

        var token = _auth.GenerateToken(user);

        return Ok(new { token });
    }
    
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = UserContext.GetUserId(User);

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized();
        
        if (!_auth.Verify(dto.CurrentPassword, user.PasswordHash))
            return Unauthorized("Current password is incorrect");
        
        user.EncryptionSalt = _auth.GenerateSalt();
        
        _auth.ChangePassword(user, dto.NewPassword);

        await _db.SaveChangesAsync();

        return NoContent();
    }
}