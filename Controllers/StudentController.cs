using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var students = await context.Students
            .AsNoTracking()
            .ToListAsync();
        return Ok(students);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var student = await context.Students.FindAsync(id);
        return student is not null ? Ok(student) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Student student)
    {
        context.Students.Add(student);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Student updated)
    {
        var student = await context.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.Name = updated.Name;
        student.GPA = updated.GPA;
        student.IsActive = updated.IsActive;

        await context.SaveChangesAsync();
        return Ok(student);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await context.Students.FindAsync(id);
        if (student is null) return NotFound();

        context.Students.Remove(student);
        await context.SaveChangesAsync();
        return NoContent();
    }
}