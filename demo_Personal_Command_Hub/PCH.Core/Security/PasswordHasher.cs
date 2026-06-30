using System.Security.Cryptography;

namespace PCH.Core.Security;

/// <summary>
/// Dependency-free password hashing using PBKDF2 (SHA-256). The hash is a
/// self-describing string: <c>v1.{iterations}.{saltBase64}.{hashBase64}</c>.
/// Used to verify the single admin password with no database or external package.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>Hashes a plaintext password into a self-describing, storable string.</summary>
    /// <param name="password">The plaintext password.</param>
    /// <returns>A versioned PBKDF2 hash string safe to store in configuration.</returns>
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    /// <summary>Verifies a plaintext password against a stored hash in constant time.</summary>
    /// <param name="password">The plaintext password to check.</param>
    /// <param name="storedHash">The stored hash produced by <see cref="Hash"/>.</param>
    /// <returns><c>true</c> if the password matches; otherwise <c>false</c>.</returns>
    public static bool Verify(string password, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        var parts = storedHash.Split('.');
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out var iterations))
            return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
