namespace Swoosh.Api.Security;

public interface IEncryptionService
{
    (string Ciphertext, int KeyVersion) Encrypt(string plaintext, Guid userId);
    string Decrypt(string ciphertext, Guid userId, int keyVersion);
}