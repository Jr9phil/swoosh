using Microsoft.AspNetCore.Mvc;
using Swoosh.Api.Data;
using Swoosh.Api.Domain;
using Swoosh.Api.Services;

namespace Swoosh.Api.Controllers;

[ApiController]
[Route("api/auth")]
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
    public async Task<IActionResult> Register(string email, string password)
    {
        if (_db.Users.Any(u => u.Email == email))
            return BadRequest("User already exists");

        var user = new User
        {
            Email = email,
            PasswordHash = _auth.HashPassword(password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok();
    }
}