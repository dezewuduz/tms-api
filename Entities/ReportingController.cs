using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/reporting")]
public class ReportingController(TmsDbContext context) : ControllerBase
{
    // 1. How many active students have GPA >= 3.0?
    [HttpGet("active-honor-students")]
    public async Task<IActionResult> GetActiveHonorStudents()
    {
        Console.WriteLine("\n>>> Query 1: Active students with GPA >= 3.0...");
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();
        return Ok(new { ActiveHonorStudents = count });
    }

    // 2. Which courses have the most enrollments?
    [HttpGet("course-enrollments")]
    public async Task<IActionResult> GetCourseEnrollments()
    {
        Console.WriteLine("\n>>> Query 2: Courses by enrollment count...");
        var list = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();
        return Ok(list);
    }

    // 3. What is the average GPA per course?
    [HttpGet("average-gpa-per-course")]
    public async Task<IActionResult> GetAverageGpaPerCourse()
    {
        Console.WriteLine("\n>>> Query 3: Average GPA per course...");
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                AverageGPA = g.Average(e => e.Student.GPA)
            })
            .ToListAsync();
        return Ok(list);
    }

    // 4A. Which students have zero enrollments? (Subquery)
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
    // 4B. Which students have zero enrollments? (LEFT JOIN)
    [HttpGet("unenrolled-students-leftjoin")]
    public async Task<IActionResult> GetUnenrolledStudentsLeftJoin()
    {
        Console.WriteLine("\n>>> Query 4B: Unenrolled students (LEFT JOIN)...");
        var list = await context.Students
            .LeftJoin(context.Enrollments,
                s => s.Id,
                e => e.StudentId,
                (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();
        return Ok(new { Approach = "LEFT JOIN", Students = list });
    }
    // TODO 1: Pagination
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
    // TODO 2: Top 5 courses
    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCourses(CancellationToken ct = default)
    {
        Console.WriteLine("\n>>> Top 5 courses by enrollment count...");
        var courses = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(5)
            .ToListAsync(ct);
        return Ok(courses);
    }
} 
