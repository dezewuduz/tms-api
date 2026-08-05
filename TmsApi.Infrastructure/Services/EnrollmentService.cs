using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt, e.Status, e.Student.Name, e.Course.Title))
            .FirstOrDefaultAsync(ct);

    public Task<List<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt, e.Status, e.Student.Name, e.Course.Title))
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

    // NEW - M9 Session 2: flat list of all enrollments across all courses
    public Task<List<EnrollmentResponseDto>> GetAllAsync(CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt, e.Status, e.Student.Name, e.Course.Title))
            .ToListAsync(ct);

    // NEW - M9 Session 2: approve a pending enrollment
    public async Task<EnrollmentResponseDto?> ApproveAsync(int id, CancellationToken ct)
    {
        var enrollment = await context.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment is null) return null;

        enrollment.Status = "Approved";
        await context.SaveChangesAsync(ct);

        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt, e.Status, e.Student.Name, e.Course.Title))
            .FirstOrDefaultAsync(ct);
    }
}