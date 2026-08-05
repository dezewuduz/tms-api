using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface IEnrollmentService
{
    Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct);
    Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct);
    Task<List<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct);
    // NEW - Exercise 2
    Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct);
    Task<Enrollment> AddAsync(Enrollment enrollment, CancellationToken ct);
    Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct);
    // NEW - M9 Session 2
    Task<List<EnrollmentResponseDto>> GetAllAsync(CancellationToken ct);
    Task<EnrollmentResponseDto?> ApproveAsync(int id, CancellationToken ct);
}