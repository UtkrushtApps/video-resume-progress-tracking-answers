using Microsoft.EntityFrameworkCore;
using StreamingProgress.Data;
using StreamingProgress.Dtos;

namespace StreamingProgress.Services;

public class PlaybackProgressService : IPlaybackProgressService
{
    private readonly StreamingDbContext _db;

    public PlaybackProgressService(StreamingDbContext db)
    {
        _db = db;
    }

    public async Task<ProgressResult> RecordProgressAsync(int videoId, UpdateProgressRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var videoDurationSeconds = await _db.Videos
            .AsNoTracking()
            .Where(v => v.Id == videoId)
            .Select(v => (int?)v.DurationSeconds)
            .SingleOrDefaultAsync(cancellationToken);

        if (videoDurationSeconds is null)
        {
            return new ProgressResult
            {
                Outcome = ProgressOutcome.VideoNotFound,
                ErrorMessage = "Video not found."
            };
        }

        if (request.PositionSeconds < 0 || request.PositionSeconds > videoDurationSeconds.Value)
        {
            return new ProgressResult
            {
                Outcome = ProgressOutcome.InvalidPosition,
                ErrorMessage = $"PositionSeconds must be between 0 and {videoDurationSeconds.Value}."
            };
        }

        var updatedAt = DateTime.UtcNow;

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
MERGE [PlaybackProgress] WITH (HOLDLOCK) AS [target]
USING (
    SELECT
        {request.UserId} AS [UserId],
        {videoId} AS [VideoId],
        {request.PositionSeconds} AS [PositionSeconds],
        {updatedAt} AS [UpdatedAt]
) AS [source]
ON [target].[UserId] = [source].[UserId]
   AND [target].[VideoId] = [source].[VideoId]
WHEN MATCHED AND [source].[PositionSeconds] > [target].[PositionSeconds] THEN
    UPDATE SET
        [PositionSeconds] = [source].[PositionSeconds],
        [UpdatedAt] = [source].[UpdatedAt]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([UserId], [VideoId], [PositionSeconds], [UpdatedAt])
    VALUES ([source].[UserId], [source].[VideoId], [source].[PositionSeconds], [source].[UpdatedAt]);",
            cancellationToken);

        var response = await _db.PlaybackProgress
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId && p.VideoId == videoId)
            .Select(p => new ProgressResponse
            {
                UserId = p.UserId,
                VideoId = p.VideoId,
                PositionSeconds = p.PositionSeconds,
                UpdatedAt = p.UpdatedAt
            })
            .SingleAsync(cancellationToken);

        return new ProgressResult
        {
            Outcome = ProgressOutcome.Saved,
            Response = response
        };
    }
}

