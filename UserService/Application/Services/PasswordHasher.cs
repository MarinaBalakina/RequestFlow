using System.Security.Cryptography;
using UserService.Application.Abstractions;
using UserService.Application.Models;

namespace UserService.Application.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public PasswordHash Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = HashPassword(password, salt);

        return new PasswordHash(
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string passwordHash, string passwordSalt)
    {
        byte[] salt = Convert.FromBase64String(passwordSalt);
        byte[] expectedHash = Convert.FromBase64String(passwordHash);
        byte[] actualHash = HashPassword(password, salt);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize
        );
    }
}