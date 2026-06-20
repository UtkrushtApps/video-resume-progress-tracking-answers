using Microsoft.EntityFrameworkCore;
using StreamingProgress.Models;

namespace StreamingProgress.Data;

public static class DbInitializer
{
    public static void Initialize(StreamingDbContext db)
    {
        db.Database.EnsureCreated();

        if (db.Videos.Any())
        {
            return;
        }

        Seed(db);
        db.SaveChanges();
    }

    public static async Task InitializeAsync(StreamingDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.Videos.AnyAsync(cancellationToken))
        {
            return;
        }

        Seed(db);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Seed(StreamingDbContext db)
    {
        var videos = new[]
        {
            new Video { Id = 1, Title = "Intro to Streaming", DurationSeconds = 600 },
            new Video { Id = 2, Title = "Advanced Encoding", DurationSeconds = 1800 },
            new Video { Id = 3, Title = "Edge Caching Basics", DurationSeconds = 900 }
        };
        db.Videos.AddRange(videos);

        var progress = new[]
        {
            new PlaybackProgress
            {
                Id = 1,
                UserId = 100,
                VideoId = 1,
                PositionSeconds = 300,
                UpdatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            },
            new PlaybackProgress
            {
                Id = 2,
                UserId = 100,
                VideoId = 2,
                PositionSeconds = 120,
                UpdatedAt = new DateTime(2024, 1, 1, 12, 5, 0, DateTimeKind.Utc)
            }
        };
        db.PlaybackProgress.AddRange(progress);
    }
}

