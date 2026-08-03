using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/enrollments")]
[Tags("Enrollments")]
[Produces("application/json")]
public class AllEnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    // GET /api/enrollments — flat list of all enrollments across all courses
    [HttpGet]
    [EndpointSummary("List all enrollments across all courses")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var enrollments = await enrollmentService.GetAllAsync(ct);
        return Ok(enrollments);
    }

    // POST /api/enrollments/{id}/approve — approve a pending enrollment
    [HttpPost("{id:int}/approve")]
    [EndpointSummary("Approve a pending enrollment")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var enrollment = await enrollmentService.ApproveAsync(id, ct);
        return enrollment is not null ? Ok(enrollment) : NotFound();
    }
}