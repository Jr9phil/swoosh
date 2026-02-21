using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Swoosh.Api.Security;

/// Service responsible for encrypting and decrypting sensitive data using AES-GCM.
/// Uses a master key from configuration and user-specific salts for key derivation.
public class EncryptionService : IEncryptionService
{
    private readonly IConfiguration _config;
    private readonly int _activeVersion;
    
    // Sentinel value used to represent null in encrypted storage
    private const string NullSentinel = "__NULL__";

    public EncryptionService(IConfiguration config)
    {
        _config = config;
        _activeVersion = int.Parse(config["Encryption:ActiveKeyVersion"]!);
    }

    /// Encrypts a string value for a specific user.
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

    /// Decrypts an encrypted string for a specific user using the specified key version.
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
    
    /// Encrypts an integer value.
    public (string Ciphertext, int KeyVersion) EncryptInt(int value, Guid userId, byte[] userSalt)
    {
        return Encrypt(value.ToString(), userId, userSalt);
    }

    /// Decrypts an integer value.
    public int DecryptInt(string encrypted, Guid userId, int keyVersion, byte[] userSalt)
    {
        var plaintext = Decrypt(encrypted, userId, keyVersion, userSalt);
        return int.Parse(plaintext);
    }

    
    /// Encrypts a nullable string, using a sentinel for null values.
    public (string Ciphertext, int KeyVersion) EncryptNullableString(
        string? value,
        Guid userId,
        byte[] userSalt)
    {
        return Encrypt(value ?? NullSentinel, userId, userSalt);
    }

    /// Decrypts a nullable string.
    public string? DecryptNullableString(
        string encrypted,
        Guid userId,
        int keyVersion,
        byte[] userSalt)
    {
        var plaintext = Decrypt(encrypted, userId, keyVersion, userSalt);
        return plaintext == NullSentinel ? null : plaintext;
    }
    
    /// Encrypts a nullable DateTime as an ISO 8601 string.
    public (string Ciphertext, int KeyVersion) EncryptNullableDateTime(
        DateTime? value,
        Guid userId,
        byte[] userSalt)
    {
        var plaintext = value == null
            ? NullSentinel
            : value.Value.ToString("O"); // ISO 8601 round-trip

        return Encrypt(plaintext, userId, userSalt);
    }

    /// Decrypts a nullable DateTime.
    public DateTime? DecryptNullableDateTime(
        string encrypted,
        Guid userId,
        int keyVersion,
        byte[] userSalt)
    {
        var plaintext = Decrypt(encrypted, userId, keyVersion, userSalt);

        if (plaintext == NullSentinel)
            return null;

        return DateTime.Parse(
            plaintext,
            null,
            DateTimeStyles.RoundtripKind
        );
    }
    
    /// Encrypts a boolean value.
    public (string Ciphertext, int KeyVersion) EncryptBool(
        bool value,
        Guid userId,
        byte[] userSalt)
    {
        return Encrypt(value ? "1" : "0", userId, userSalt);
    }

    /// Decrypts a boolean value.
    public bool DecryptBool(
        string encrypted,
        Guid userId,
        int keyVersion,
        byte[] userSalt)
    {
        return Decrypt(encrypted, userId, keyVersion, userSalt) == "1";
    }
}