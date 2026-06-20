namespace StreamingProgress.Models;

public class PlaybackProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int VideoId { get; set; }
    public int PositionSeconds { get; set; }
    public DateTime UpdatedAt { get; set; }
}

