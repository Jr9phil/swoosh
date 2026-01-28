using System.Security.Cryptography;
using System.Text;

namespace Swoosh.Api.Security;

public class EncryptionService : IEncryptionService
{
    private readonly string _masterKey;

    public EncryptionService(IConfiguration config)
    {
        _masterKey = config["Encryption:MasterKey"]
                     ?? throw new Exception("Encryption master key missing");
    }

    public string Encrypt(string plaintext, Guid userId)
    {
        var key = KeyDerivation.DeriveKey(_masterKey, userId);

        using var aes = new AesGcm(key);
        var nonce = RandomNumberGenerator.GetBytes(12);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return Convert.ToBase64String(
            nonce
                .Concat(tag)
                .Concat(ciphertext)
                .ToArray()
        );
    }

    public string Decrypt(string encrypted, Guid userId)
    {
        try
        {
            var key = KeyDerivation.DeriveKey(_masterKey, userId);
            var data = Convert.FromBase64String(encrypted);

            var nonce = data[..12];
            var tag = data[12..28];
            var ciphertext = data[28..];

            using var aes = new AesGcm(key);
            var plaintext = new byte[ciphertext.Length];

            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (FormatException e)
        {
            throw new CryptographicException("Stored data is not valid encrypted content", e);
        }

    }
}