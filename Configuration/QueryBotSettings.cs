namespace QueryBot.Configuration;

public sealed class QueryBotSettings
{
    /// <summary>
    /// Filesystem path where uploaded model documents are saved.
    /// Dev: S:/Quex/Dev/UploadedQueryBotDocs
    /// Prod: /opt/quex/attachments (shared with the worker via qb_attachments volume)
    /// </summary>
    public string AttachmentStoragePath { get; set; } = string.Empty;
}
