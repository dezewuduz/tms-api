/*using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var courses = await context.Courses
            .AsNoTracking()
            .Select(c => new { c.Id, c.Title, c.Code, c.MaxCapacity })
            .ToListAsync();
        return Ok(courses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return course is not null ? Ok(course) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request)
    {
        var course = new Course { Title = request.Title, Code = request.Code, MaxCapacity = request.MaxCapacity };
        context.Courses.Add(course);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, course);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await context.Courses.FindAsync(id);
        if (course is null) return NotFound();
        context.Courses.Remove(course);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
public record CreateCourseRequest(string Title, string Code, int MaxCapacity);*/
//------- /------
using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseService courseService) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);
        return course is not null ? Ok(course) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken ct)
    {
        if (await courseService.CodeExistsAsync(request.Code, ct))
            return Conflict(new ProblemDetails
            {
                Title = "Course code already exists",
                Detail = $"A course with code '{request.Code}' is already registered.",
                Status = StatusCodes.Status409Conflict
            });

        var result = await courseService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }
}