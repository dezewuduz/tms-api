using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(ICachedCourseService cachedCourseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var result = await cachedCourseService.GetCoursesPageAsync(page, pageSize, ct);

        var totalPages = result.TotalPages;
        var hasNext = result.HasNext;
        var hasPrevious = result.HasPrevious;

        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                totalCount = result.TotalCount,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages,
                hasNext,
                hasPrevious
            },
            links = new
            {
                self = $"/api/v2/courses?page={page}&pageSize={pageSize}",
                next = hasNext ? $"/api/v2/courses?page={page + 1}&pageSize={pageSize}" : (string?)null,
                prev = hasPrevious ? $"/api/v2/courses?page={page - 1}&pageSize={pageSize}" : (string?)null,
                enroll = "/api/v2/enrollments"
            }
        });
    }
}