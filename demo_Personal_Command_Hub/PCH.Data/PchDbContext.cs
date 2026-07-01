using Microsoft.EntityFrameworkCore;
using PCH.Core.Models;

namespace PCH.Data;

/// <summary>
/// Entity Framework Core context for the PCH SQLite database.
/// Table names follow the plural snake_case convention (tasks, news_items, bookings).
/// </summary>
public class PchDbContext : DbContext
{
    public PchDbContext(DbContextOptions<PchDbContext> options) : base(options)
    {
    }

    /// <summary>Tasks shown on the dashboard.</summary>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    /// <summary>News articles fetched from RSS feeds.</summary>
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();

    /// <summary>Reservations/appointments.</summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>Emails fetched from the IMAP inbox.</summary>
    public DbSet<Email> Emails => Set<Email>();

    /// <summary>Configurable RSS feed sources.</summary>
    public DbSet<RssFeed> RssFeeds => Set<RssFeed>();

    /// <summary>Single-row app settings (IMAP config, notification prefs).</summary>
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(e =>
        {
            e.ToTable("tasks");
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(256);
            e.HasIndex(t => t.ExternalId);
        });

        modelBuilder.Entity<NewsItem>(e =>
        {
            e.ToTable("news_items");
            e.HasKey(n => n.Id);
            e.Property(n => n.Title).IsRequired().HasMaxLength(512);
            e.Property(n => n.Link).IsRequired();
            e.HasIndex(n => n.Link).IsUnique();
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.ToTable("bookings");
            e.HasKey(b => b.Id);
            e.Property(b => b.Title).IsRequired().HasMaxLength(256);
            e.Property(b => b.Platform).HasMaxLength(64);
            e.HasIndex(b => b.ExternalId);
        });

        modelBuilder.Entity<Email>(e =>
        {
            e.ToTable("emails");
            e.HasKey(em => em.Id);
            e.Property(em => em.MessageId).IsRequired().HasMaxLength(512);
            e.Property(em => em.Subject).IsRequired().HasMaxLength(1000);
            e.Property(em => em.Sender).IsRequired().HasMaxLength(512);
            e.Property(em => em.BodyPreview).IsRequired();
            e.HasIndex(em => em.MessageId).IsUnique();
        });

        modelBuilder.Entity<RssFeed>(e =>
        {
            e.ToTable("rss_feeds");
            e.HasKey(f => f.Id);
            e.Property(f => f.Url).IsRequired();
            e.Property(f => f.Category).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<AppSettings>(e =>
        {
            e.ToTable("app_settings");
            e.HasKey(a => a.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}
