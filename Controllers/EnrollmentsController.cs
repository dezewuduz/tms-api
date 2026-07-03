using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
public class EnrollmentsController(
    ICourseService courseService,
    IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
    public async Task<IActionResult> GetEnrollment(int courseId, int id, CancellationToken ct)
    {
        var enrollment = await enrollmentService.GetByIdAsync(courseId, id, ct);
        return enrollment is not null ? Ok(enrollment) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> EnrollStudent(int courseId, [FromBody] EnrollStudentRequest request, CancellationToken ct)
    {
        // Step 1: Course already exists? → 404
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null)
            return NotFound();

        // Step 2: Course fully enrolled? → 409
        if (course.EnrollmentCount >= course.MaxCapacity)
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
                Status = StatusCodes.Status409Conflict
            });

        // Step 3: Enroll → 201
        try
        {
            var enrollment = await enrollmentService.CreateAsync(courseId, request, ct);
            return CreatedAtAction(nameof(GetEnrollment), new { courseId, id = enrollment.Id }, enrollment);
        }
        catch (DuplicateEnrollmentException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Student already enrolled",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}