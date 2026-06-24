using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
        var query = context.Students.Where(s => s.GPA >= 3.0m);
        Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);
        Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
        var results = orderedQuery.ToList();
        Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");
        return Ok(results);
    }

    private static bool IsHonorRoll(decimal gpa) => gpa >= 3.5m;

    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
        try
        {
            var students = context.Students.Where(s => IsHonorRoll(s.GPA)).ToList();
            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }
    [HttpGet("translation-fix-server")]
    public IActionResult TestServerSide()
    {
        Console.WriteLine("\n>>> Server-Side Evaluation...");
        var students = context.Students.Where(s => s.GPA >= 3.5m).ToList();
        return Ok(students);
    }

    [HttpGet("translation-fix-client")]
    public IActionResult TestClientSide()
    {
        Console.WriteLine("\n>>> Client-Side Evaluation...");
        var students = context.Students.AsEnumerable().Where(s => IsHonorRoll(s.GPA)).ToList();
        return Ok(students);
    }

    [HttpGet("n-plus-one")]
    public async Task<IActionResult> TestNPlusOne(CancellationToken ct)
    {
        Console.WriteLine("\n>>> Part A: N+1 Query (BAD)...");
        var students = await context.Students.AsNoTracking().ToListAsync(ct);
        foreach (var s in students)
        {
            var count = await context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id, ct);
            Console.WriteLine($"  {s.Name}: {count} enrollments");
        }
        return Ok("Check console for N+1 queries!");
    }

    [HttpGet("n-plus-one-fix")]
    public async Task<IActionResult> TestNPlusOneFix(CancellationToken ct)
    {
        Console.WriteLine("\n>>> Part B: Shaped Query (GOOD)...");
        var report = await context.Students
            .AsNoTracking()
            .Select(s => new { s.Name, EnrollmentCount = s.Enrollments.Count })
            .ToListAsync(ct);
        foreach (var r in report)
            Console.WriteLine($"  {r.Name}: {r.EnrollmentCount} enrollments");
        return Ok(report);
    }

    // Exercise 8 — Shadow Property LastUpdated
    [HttpPut("students/{id}/name")]
    public async Task<IActionResult> UpdateStudentName(
        int id, [FromBody] string newName, CancellationToken ct)
    {
        var student = await context.Students.FindAsync([id], ct);
        if (student is null) return NotFound();

        student.Name = newName;
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return Ok(new { student.Id, student.Name, LastUpdated = DateTime.UtcNow });
    }
    //  Exercise 8 — Concurrency test
    [HttpPut("students/{id}/gpa")]
    public async Task<IActionResult> UpdateStudentGpa(
        int id, [FromBody] decimal newGpa, CancellationToken ct)
    {
        try
        {
            var student = await context.Students.FindAsync([id], ct);
            if (student is null) return NotFound();

            student.GPA = newGpa;
            context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);
            return Ok(new { student.Id, student.GPA });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Console.WriteLine($">>> CONCURRENCY EXCEPTION: {ex.Message}");
            return Conflict(new { Message = "Concurrency conflict detected!" });
        }
    }
}