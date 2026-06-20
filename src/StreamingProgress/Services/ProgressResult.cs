namespace StreamingProgress.Services;

public enum ProgressOutcome
{
    Saved,
    VideoNotFound,
    InvalidPosition
}

public class ProgressResult
{
    public ProgressOutcome Outcome { get; set; }
    public Dtos.ProgressResponse? Response { get; set; }
    public string? ErrorMessage { get; set; }
}

