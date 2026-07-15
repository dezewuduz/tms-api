using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt))
            .FirstOrDefaultAsync(ct);
    public Task<List<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt))
            .ToListAsync(ct);

    public async Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        var alreadyEnrolled = await context.Enrollments
            .AnyAsync(e => e.CourseId == courseId && e.StudentId == request.StudentId, ct);

        if (alreadyEnrolled)
        {
            logger.LogWarning("Student {StudentId} is already enrolled in course {CourseId}", request.StudentId, courseId);
            throw new DuplicateEnrollmentException(request.StudentId, courseId);
        }

        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };
        context.Enrollments.Add(enrollment);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Race condition safety net: two concurrent requests both passed the AnyAsync check above
            logger.LogWarning("Race condition on enrollment insert for student {StudentId} in course {CourseId}", request.StudentId, courseId);
            throw new DuplicateEnrollmentException(request.StudentId, courseId);
        }

        logger.LogInformation("Enrolled student {StudentId} into course {CourseId}", request.StudentId, courseId);
        return (await GetByIdAsync(courseId, enrollment.Id, ct))!;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: "23505" };
         // NEW - Exercise 2
    public async Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct) =>
        await context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == studentId && e.Course.Code == courseCode, ct);

    public async Task<Enrollment> AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
        return enrollment;
    }
    public async Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct) =>
        await context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);
}
