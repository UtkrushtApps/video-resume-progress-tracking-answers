namespace StreamingProgress.Dtos;

public class UpdateProgressRequest
{
    public int UserId { get; set; }
    public int PositionSeconds { get; set; }
}

