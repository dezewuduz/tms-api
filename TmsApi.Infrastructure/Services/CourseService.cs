using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Services;

public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);
public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, string instructorId, CancellationToken ct)
{
    var course = new Course
    {
        Code = request.Code,
        Title = request.Title,
        MaxCapacity = request.MaxCapacity,
        InstructorId = instructorId
    };
    context.Courses.Add(course);
    await context.SaveChangesAsync(ct);
    logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
    return (await GetByIdAsync(course.Id, ct))!;
}
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);

    public async Task<Course?> GetByCodeAsync(string code, CancellationToken ct) =>
    await context.Courses
        .Include(c => c.Enrollments)
        .FirstOrDefaultAsync(c => c.Code == code, ct);
    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
{
    // TODO 1
    IQueryable<Course> query = context.Courses.AsNoTracking();

    // TODO 2
    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(c =>
            EF.Functions.ILike(c.Title, $"%{request.Search}%")
            || EF.Functions.ILike(c.Code, $"%{request.Search}%"));
    }
    // TODO 3 — Count BEFORE Skip/Take (-5 deduction)
    var totalCount = await query.CountAsync(ct);
    // TODO 4 — OrderBy whitelist
    IQueryable<Course> sortedQuery = request.OrderBy switch
    {
        "Code" => request.Descending
            ? query.OrderByDescending(c => c.Code)
            : query.OrderBy(c => c.Code),
        "MaxCapacity" => request.Descending
            ? query.OrderByDescending(c => c.MaxCapacity)
            : query.OrderBy(c => c.MaxCapacity),
        _ => request.Descending
            ? query.OrderByDescending(c => c.Title)
            : query.OrderBy(c => c.Title)
    };
    // TODO 5 — Skip, Take, Select (projection SQL ), ToListAsync
    var items = await sortedQuery
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(c => new CourseResponseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
        .ToListAsync(ct);

    // TODO 6
    return new PagedResponse<CourseResponseDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}

    public Task<Course?> GetEntityByIdAsync(int id, CancellationToken ct) =>
        context.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<CourseResponseDto?> UpdateAsync(int id, CreateCourseRequest request, CancellationToken ct)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null) return null;

        course.Code = request.Code;
        course.Title = request.Title;
        course.MaxCapacity = request.MaxCapacity;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Updated course {CourseId} ({Code})", course.Id, course.Code);

        return await GetByIdAsync(course.Id, ct);
    }
}