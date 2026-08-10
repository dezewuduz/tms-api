using MediatR;
using TmsApi.Application.Common;
using TmsApi.Application.DTOs;

namespace TmsApi.Application.Enrollments.Commands;

public record ApproveEnrollmentCommand(int Id)
    : IRequest<Result<EnrollmentResponseDto, EnrollmentError>>;