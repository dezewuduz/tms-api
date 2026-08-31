using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Authorization;

public class CourseOwnerAuthorizationHandler : AuthorizationHandler<CourseOwnerRequirement, Course>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CourseOwnerRequirement requirement,
        Course resource)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null && userId == resource.InstructorId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}