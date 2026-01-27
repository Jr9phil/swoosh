using Microsoft.AspNetCore.Mvc;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Services;
using Swoosh.Api.Dtos;

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
            PasswordHash = _auth.HashPassword(dto.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok();
    }
    
    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        var user = _db.Users.SingleOrDefault(u => u.Email == dto.Email);
        if (user == null)
            return Unauthorized("Invalid credentials");

        if (!_auth.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var token = _auth.GenerateToken(user);

        return Ok(new { token });
    }
}