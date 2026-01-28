namespace Swoosh.Api.Security;

public interface IEncryptionService
{
    string Encrypt(string plaintext, Guid userId);
    string Decrypt(string ciphertext, Guid userId);
}