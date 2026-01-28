namespace Swoosh.Api.Security;

public interface IEncryptionService
{
    (string Ciphertext, int KeyVersion) Encrypt(string plaintext, Guid userId, byte[] salt);
    string Decrypt(string ciphertext, Guid userId, int keyVersion, byte[] salt);
}