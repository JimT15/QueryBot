using Microsoft.EntityFrameworkCore;
using QueryBot.Data.Entities;

namespace QueryBot.Data;

public sealed class QueryBotDbContext : DbContext
{
    public QueryBotDbContext(DbContextOptions<QueryBotDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(x => x.Nickname)
                .HasColumnName("nickname")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Password)
                .HasColumnName("password")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.System)
                .HasColumnName("system")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.CreatedUtc)
                .HasColumnName("created_utc")
                .IsRequired();

            entity.Property(x => x.UpdatedUtc)
                .HasColumnName("updated_utc")
                .IsRequired();

            entity.HasIndex(x => new { x.Email, x.System })
                .HasDatabaseName("IX_user_email_system")
                .IsUnique();
        });
    }
}
