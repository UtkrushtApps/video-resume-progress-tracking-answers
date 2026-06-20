namespace StreamingProgress.Dtos;

public class ProgressResponse
{
    public int UserId { get; set; }
    public int VideoId { get; set; }
    public int PositionSeconds { get; set; }
    public DateTime UpdatedAt { get; set; }
}

