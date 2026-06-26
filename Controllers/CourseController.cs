using Microsoft.AspNetCore.Mvc;
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
            .Select(c => new { c.Id, c.Title, c.Code, c.Capacity })
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
        var course = new Course { Title = request.Title, Code = request.Code, Capacity = request.Capacity };
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

public record CreateCourseRequest(string Title, string Code, int Capacity);