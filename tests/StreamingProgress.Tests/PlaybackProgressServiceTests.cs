using Microsoft.EntityFrameworkCore;
using StreamingProgress.Data;
using StreamingProgress.Dtos;
using StreamingProgress.Models;
using StreamingProgress.Services;
using Xunit;

namespace StreamingProgress.Tests;

public class PlaybackProgressServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1,1433;Database=StreamingDb;User Id=sa;Password=Your_strong_Pass123;TrustServerCertificate=True;Encrypt=False;";

    private static StreamingDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<StreamingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new StreamingDbContext(options);
    }

    public PlaybackProgressServiceTests()
    {
        using var db = NewContext();
        db.Database.EnsureCreated();
        DbInitializer.Initialize(db);
    }

    private static async Task ResetProgressForUserVideoAsync(int userId, int videoId, int? seedPosition, DateTime? seedUpdatedAt)
    {
        using var db = NewContext();
        var rows = await db.PlaybackProgress
            .Where(p => p.UserId == userId && p.VideoId == videoId)
            .ToListAsync();
        db.PlaybackProgress.RemoveRange(rows);
        await db.SaveChangesAsync();

        if (seedPosition.HasValue)
        {
            db.PlaybackProgress.Add(new PlaybackProgress
            {
                UserId = userId,
                VideoId = videoId,
                PositionSeconds = seedPosition.Value,
                UpdatedAt = seedUpdatedAt ?? DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task RepeatedUpdates_KeepSingleRow_PerUserVideo()
    {
        const int userId = 700;
        const int videoId = 3;
        await ResetProgressForUserVideoAsync(userId, videoId, null, null);

        for (var i = 1; i <= 5; i++)
        {
            using var db = NewContext();
            var service = new PlaybackProgressService(db);
            await service.RecordProgressAsync(videoId,
                new UpdateProgressRequest { UserId = userId, PositionSeconds = i * 10 },
                CancellationToken.None);
        }

        using var verify = NewContext();
        var count = await verify.PlaybackProgress
            .CountAsync(p => p.UserId == userId && p.VideoId == videoId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task OlderPosition_DoesNotMoveProgressBackwards()
    {
        const int userId = 701;
        const int videoId = 3;
        await ResetProgressForUserVideoAsync(userId, videoId, 500, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        using (var db = NewContext())
        {
            var service = new PlaybackProgressService(db);
            await service.RecordProgressAsync(videoId,
                new UpdateProgressRequest { UserId = userId, PositionSeconds = 100 },
                CancellationToken.None);
        }

        using var verify = NewContext();
        var stored = await verify.PlaybackProgress
            .SingleAsync(p => p.UserId == userId && p.VideoId == videoId);
        Assert.Equal(500, stored.PositionSeconds);
    }

    [Fact]
    public async Task NewerPosition_UpdatesProgressForward()
    {
        const int userId = 702;
        const int videoId = 3;
        await ResetProgressForUserVideoAsync(userId, videoId, 100, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        using (var db = NewContext())
        {
            var service = new PlaybackProgressService(db);
            await service.RecordProgressAsync(videoId,
                new UpdateProgressRequest { UserId = userId, PositionSeconds = 250 },
                CancellationToken.None);
        }

        using var verify = NewContext();
        var stored = await verify.PlaybackProgress
            .SingleAsync(p => p.UserId == userId && p.VideoId == videoId);
        Assert.Equal(250, stored.PositionSeconds);
    }

    [Fact]
    public async Task NegativePosition_IsRejected_AndNotPersisted()
    {
        const int userId = 703;
        const int videoId = 3;
        await ResetProgressForUserVideoAsync(userId, videoId, null, null);

        using (var db = NewContext())
        {
            var service = new PlaybackProgressService(db);
            var result = await service.RecordProgressAsync(videoId,
                new UpdateProgressRequest { UserId = userId, PositionSeconds = -5 },
                CancellationToken.None);
            Assert.Equal(ProgressOutcome.InvalidPosition, result.Outcome);
        }

        using var verify = NewContext();
        var count = await verify.PlaybackProgress
            .CountAsync(p => p.UserId == userId && p.VideoId == videoId);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task PositionBeyondDuration_IsRejected_AndNotPersisted()
    {
        const int userId = 704;
        const int videoId = 3; // duration 900
        await ResetProgressForUserVideoAsync(userId, videoId, null, null);

        using (var db = NewContext())
        {
            var service = new PlaybackProgressService(db);
            var result = await service.RecordProgressAsync(videoId,
                new UpdateProgressRequest { UserId = userId, PositionSeconds = 5000 },
                CancellationToken.None);
            Assert.Equal(ProgressOutcome.InvalidPosition, result.Outcome);
        }

        using var verify = NewContext();
        var count = await verify.PlaybackProgress
            .CountAsync(p => p.UserId == userId && p.VideoId == videoId);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task UnknownVideo_ReturnsNotFound_AndCreatesNoRow()
    {
        const int userId = 705;
        const int videoId = 99999;
        await ResetProgressForUserVideoAsync(userId, videoId, null, null);

        using (var db = NewContext())
        {
            var service = new PlaybackProgressService(db);
            var result = await service.RecordProgressAsync(videoId,
                new UpdateProgressRequest { UserId = userId, PositionSeconds = 50 },
                CancellationToken.None);
            Assert.Equal(ProgressOutcome.VideoNotFound, result.Outcome);
        }

        using var verify = NewContext();
        var count = await verify.PlaybackProgress
            .CountAsync(p => p.UserId == userId && p.VideoId == videoId);
        Assert.Equal(0, count);
    }
}

