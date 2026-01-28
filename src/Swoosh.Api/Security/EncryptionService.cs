using System.Security.Cryptography;
using System.Text;

namespace Swoosh.Api.Security;

public class EncryptionService : IEncryptionService
{
    private readonly IConfiguration _config;
    private readonly int _activeVersion;

    public EncryptionService(IConfiguration config)
    {
        _config = config;
        _activeVersion = int.Parse(config["Encryption:ActiveKeyVersion"]!);
    }

    public (string Ciphertext, int KeyVersion) Encrypt(string plaintext, Guid userId, byte[] userSalt)
    {
        var masterKey = _config[$"Encryption:Keys:{_activeVersion}"]!;
        var key = KeyDerivation.DeriveKey(masterKey, userId, userSalt);

        using var aes = new AesGcm(key);
        var nonce = RandomNumberGenerator.GetBytes(12);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var combined = Convert.ToBase64String(
            nonce.Concat(tag).Concat(ciphertext).ToArray()
        );

        return (combined, _activeVersion);
    }


    public string Decrypt(string encrypted, Guid userId, int keyVersion, byte[] userSalt)
    {
        try
        {
            var masterKey = _config[$"Encryption:Keys:{keyVersion}"]!;
            var key = KeyDerivation.DeriveKey(masterKey, userId, userSalt);

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