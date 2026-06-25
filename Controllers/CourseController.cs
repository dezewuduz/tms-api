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
            .ToListAsync();
        return Ok(courses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await context.Courses.FindAsync(id);
        return course is not null ? Ok(course) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Course course)
    {
        context.Courses.Add(course);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, course);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Course updated)
    {
        var course = await context.Courses.FindAsync(id);
        if (course is null) return NotFound();

        course.Title = updated.Title;
        course.Code = updated.Code;
        course.Capacity = updated.Capacity;

        await context.SaveChangesAsync();
        return Ok(course);
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