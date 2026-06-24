using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
namespace TmsApi.Controllers;
[ApiController]
[Route("api/reporting")]
public class ReportingController(TmsDbContext context) : ControllerBase
{
    [HttpGet("active-honor-students")]
    public async Task<IActionResult> GetActiveHonorStudents()
    {
        Console.WriteLine("\n>>> Query 1: Active students with GPA >= 3.0...");
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();
        return Ok(new { ActiveHonorStudents = count });
    }

    [HttpGet("course-enrollments")]
    public async Task<IActionResult> GetCourseEnrollments()
    {
        Console.WriteLine("\n>>> Query 2: Courses by enrollment count...");
        var list = await context.Courses
            .Select(c => new { c.Title, EnrollmentCount = c.Enrollments.Count })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("average-gpa-per-course")]
    public async Task<IActionResult> GetAverageGpaPerCourse()
    {
        Console.WriteLine("\n>>> Query 3: Average GPA per course...");
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new { Course = g.Key, AverageGPA = g.Average(e => e.Student.GPA) })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("unenrolled-students-subquery")]
    public async Task<IActionResult> GetUnenrolledStudentsSubquery()
    {
        Console.WriteLine("\n>>> Query 4A: Unenrolled students (NOT EXISTS)...");
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();
        return Ok(new { Approach = "NOT EXISTS", Students = list });
    }

    [HttpGet("unenrolled-students-leftjoin")]
    public async Task<IActionResult> GetUnenrolledStudentsLeftJoin()
    {
        Console.WriteLine("\n>>> Query 4B: Unenrolled students (LEFT JOIN)...");
        var list = await context.Students
            .LeftJoin(context.Enrollments,
                s => s.Id, e => e.StudentId,
                (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();
        return Ok(new { Approach = "LEFT JOIN", Students = list });
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        Console.WriteLine($"\n>>> Pagination: page={page}, pageSize={pageSize}");
        var students = await context.Students
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return Ok(students);
    }

    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCourses(CancellationToken ct = default)
    {
        Console.WriteLine("\n>>> Top 5 courses by enrollment count...");
        var courses = await context.Courses
            .Select(c => new { c.Title, EnrollmentCount = c.Enrollments.Count })
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(5)
            .ToListAsync(ct);
        return Ok(courses);
    }
    // Exercise 9 — Bulk Archive
    [HttpPost("archive-old-enrollments")]
    public async Task<IActionResult> ArchiveOldEnrollments(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-1);
        Console.WriteLine($"\n>>> Bulk archive enrollments before {cutoff}...");

        var count = await context.Enrollments
            .Where(e => e.EnrolledAt < cutoff)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsArchived, true), ct);

        return Ok(new { ArchivedCount = count });
    }

    // Exercise 9 — Soft Delete
    [HttpDelete("students/{id}")]
    public async Task<IActionResult> SoftDeleteStudent(int id, CancellationToken ct)
    {
        var student = await context.Students.FindAsync([id], ct);
        if (student is null) return NotFound();

        student.IsDeleted = true;
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return Ok(new { Message = $"Student {id} soft deleted " });
    }

    // Exercise 9 — Normal query (IsDeleted hidden)
    [HttpGet("students/active")]
    public async Task<IActionResult> GetActiveStudents(CancellationToken ct)
    {
        Console.WriteLine("\n>>> Normal query — IsDeleted students hidden...");
        var students = await context.Students
            .AsNoTracking()
            .Select(s => new { s.Id, s.Name, s.IsDeleted })
            .ToListAsync(ct);
        return Ok(students);
    }

    // Exercise 9 — Admin (IgnoreQueryFilters)
    [HttpGet("students/all-including-deleted")]
    public async Task<IActionResult> GetAllStudentsAdmin(CancellationToken ct)
    {
        Console.WriteLine("\n>>> Admin: IgnoreQueryFilters...");
        var students = await context.Students
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(s => new { s.Id, s.Name, s.IsDeleted })
            .ToListAsync(ct);
        return Ok(students);
    }
}