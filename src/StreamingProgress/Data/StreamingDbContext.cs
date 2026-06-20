using Microsoft.EntityFrameworkCore;
using StreamingProgress.Models;

namespace StreamingProgress.Data;

public class StreamingDbContext : DbContext
{
    public StreamingDbContext(DbContextOptions<StreamingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Video> Videos => Set<Video>();
    public DbSet<PlaybackProgress> PlaybackProgress => Set<PlaybackProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Title).IsRequired().HasMaxLength(200);
            entity.Property(v => v.DurationSeconds).IsRequired();
        });

        modelBuilder.Entity<PlaybackProgress>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.VideoId).IsRequired();
            entity.Property(p => p.PositionSeconds).IsRequired();
            entity.Property(p => p.UpdatedAt).IsRequired();

            entity.HasIndex(p => new { p.UserId, p.VideoId })
                .IsUnique()
                .HasDatabaseName("UX_PlaybackProgress_UserId_VideoId");
        });

        base.OnModelCreating(modelBuilder);
    }
}

