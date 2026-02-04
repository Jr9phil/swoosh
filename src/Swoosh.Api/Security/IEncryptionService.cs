namespace Swoosh.Api.Security;

public interface IEncryptionService
{
    (string Ciphertext, int KeyVersion) Encrypt(string plaintext, Guid userId, byte[] salt);
    string Decrypt(string ciphertext, Guid userId, int keyVersion, byte[] salt);
    (string Ciphertext, int KeyVersion) EncryptNullableString(string? plaintext, Guid userId, byte[] salt);
    string? DecryptNullableString(string ciphertext, Guid userId, int keyVersion, byte[] salt);
    (string Ciphertext, int KeyVersion) EncryptNullableDateTime(DateTime? value, Guid userId, byte[] salt);
    DateTime? DecryptNullableDateTime(string ciphertext, Guid userId, int keyVersion, byte[] salt);
    (string Ciphertext, int KeyVersion) EncryptBool(bool value, Guid userId, byte[] salt);
    bool DecryptBool(string ciphertext, Guid userId, int keyVersion, byte[] salt);
}