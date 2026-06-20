using Microsoft.AspNetCore.Mvc;
using StreamingProgress.Dtos;
using StreamingProgress.Services;

namespace StreamingProgress.Controllers;

[ApiController]
[Route("api/videos")]
public class ProgressController : ControllerBase
{
    private readonly IPlaybackProgressService _service;

    public ProgressController(IPlaybackProgressService service)
    {
        _service = service;
    }

    [HttpPost("{videoId}/progress")]
    public async Task<IActionResult> RecordProgress(int videoId, [FromBody] UpdateProgressRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        try
        {
            var result = await _service.RecordProgressAsync(videoId, request, cancellationToken);

            return result.Outcome switch
            {
                ProgressOutcome.Saved when result.Response is not null => Ok(result.Response),
                ProgressOutcome.VideoNotFound => NotFound(new { error = result.ErrorMessage ?? "Video not found." }),
                ProgressOutcome.InvalidPosition => BadRequest(new { error = result.ErrorMessage ?? "Invalid PositionSeconds." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "An unexpected error occurred while recording playback progress." })
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred while recording playback progress." });
        }
    }
}

