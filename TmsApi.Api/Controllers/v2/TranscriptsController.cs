using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/transcripts")]
[ApiVersion("2.0")]
public class TranscriptsController : ControllerBase
{
    [HttpPost]
[EnableRateLimiting("transcripts")]
public async Task<IActionResult> RequestTranscript([FromBody] object? _)
{
    await Task.Delay(2000); // temporary: simulate work so concurrency limiter is observable
    return Ok();
}
} 
