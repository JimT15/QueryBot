namespace QueryBot.Data.Entities;

public sealed class User
{
    public long Id { get; set; }

    public string Email { get; set; } = null!;

    public string Nickname { get; set; } = null!;

    /// <summary>
    /// Stores a one-way PBKDF2-SHA256 hash of the password. Never plaintext.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// Identifies which Quex system this login belongs to (e.g. "querybot").
    /// </summary>
    public string System { get; set; } = null!;

    /// <summary>
    /// The platform client id associated with this QueryBot user. Null for legacy rows.
    /// </summary>
    public long? ClientId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
