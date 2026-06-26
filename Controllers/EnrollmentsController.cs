using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController(TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await context.Enrollments
            .AsNoTracking()
            .Select(e => new
            {
                e.Id,
                e.StudentId,
                e.CourseId,
                e.Grade,
                e.EnrolledAt,
                e.IsArchived,
                StudentName = e.Student.Name,
                CourseTitle = e.Course.Title
            })
            .ToListAsync();
        return Ok(enrollments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var enrollment = await context.Enrollments
            .AsNoTracking()
            .Select(e => new
            {
                e.Id,
                e.StudentId,
                e.CourseId,
                e.Grade,
                e.EnrolledAt,
                e.IsArchived,
                StudentName = e.Student.Name,
                CourseTitle = e.Course.Title
            })
            .FirstOrDefaultAsync(e => e.Id == id);
        return enrollment is not null ? Ok(enrollment) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
    {
        var enrollment = new Enrollment
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            EnrolledAt = DateTime.UtcNow
        };
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = enrollment.Id }, enrollment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var enrollment = await context.Enrollments.FindAsync(id);
        if (enrollment is null) return NotFound();
        context.Enrollments.Remove(enrollment);
        await context.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateEnrollmentRequest(int StudentId, int CourseId);