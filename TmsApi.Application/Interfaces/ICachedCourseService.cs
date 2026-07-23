using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface ICachedCourseService
{
    Task<PagedResponse<CourseResponseDto>> GetCoursesPageAsync(int page, int pageSize, CancellationToken ct);
    Task InvalidateCourseCacheAsync(CancellationToken ct);
}