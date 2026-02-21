using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Swoosh.Api.Domain;

namespace Swoosh.Api.Services;

/// Service providing authentication-related functionality, including password hashing and JWT generation.
public class AuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    /// Hashes a plaintext password using BCrypt.
    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    /// Verifies a plaintext password against a BCrypt hash.
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
    
    /// Generates a cryptographically secure random salt.
    public byte[] GenerateSalt(int size = 16) 
        => RandomNumberGenerator.GetBytes(size);
    

    /// Generates a JWT token for the specified user containing their ID and email.
    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// Updates the password hash for a user.
    public void ChangePassword(User user, string newPassword)
    {
        user.PasswordHash = HashPassword(newPassword);
    }
}