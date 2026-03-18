using Microsoft.EntityFrameworkCore;
using QueryBot.Data;
using QueryBot.Security;

namespace QueryBot.Auth;

public sealed class QueryBotAuthService
{
    private readonly QueryBotDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public QueryBotAuthService(QueryBotDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Returns (Email, Nickname) if the credentials are valid for system="querybot".
    /// Returns null if not found or password is incorrect.
    /// </summary>
    public async Task<(string Email, string Nickname)?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.System == "querybot", ct);

        if (user is null)
            return null;

        if (!_passwordHasher.VerifyPassword(password, user.Password))
            return null;

        return (user.Email, user.Nickname);
    }
}
