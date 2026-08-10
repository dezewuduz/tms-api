using MediatR;
using TmsApi.Application.Common;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Commands;

public class ApproveEnrollmentHandler(IEnrollmentService enrollmentService)
    : IRequestHandler<ApproveEnrollmentCommand, Result<EnrollmentResponseDto, EnrollmentError>>
{
    public async Task<Result<EnrollmentResponseDto, EnrollmentError>> Handle(
        ApproveEnrollmentCommand command, CancellationToken ct)
    {
        var updated = await enrollmentService.ApproveAsync(command.Id, ct);

        if (updated is null)
            return Result<EnrollmentResponseDto, EnrollmentError>.Failure(
                EnrollmentError.NotFound(command.Id));

        return Result<EnrollmentResponseDto, EnrollmentError>.Success(updated);
    }
}