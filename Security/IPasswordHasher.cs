namespace QueryBot.Security;

public interface IPasswordHasher
{
    /// <summary>
    /// Returns a salted PBKDF2-SHA256 hash of <paramref name="password"/> suitable for storage.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Returns true if <paramref name="password"/> matches <paramref name="storedHash"/>.
    /// </summary>
    bool VerifyPassword(string password, string storedHash);
}
