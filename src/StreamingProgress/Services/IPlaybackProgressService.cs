namespace StreamingProgress.Services;

public interface IPlaybackProgressService
{
    Task<ProgressResult> RecordProgressAsync(int videoId, Dtos.UpdateProgressRequest request, CancellationToken cancellationToken);
}

