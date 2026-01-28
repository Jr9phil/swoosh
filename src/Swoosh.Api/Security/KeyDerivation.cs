using System.Security.Cryptography;
using System.Text;

namespace Swoosh.Api.Security;

public static class KeyDerivation
{
    public static byte[] DeriveKey(string masterKey, Guid userId)
    {
        using var hmac = new HMACSHA256(Convert.FromBase64String(masterKey));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(userId.ToString()));
    }
}