using Microsoft.AspNetCore.Authorization;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Api.Authorization;

public class ProjectMembershipRequirement : IAuthorizationRequirement { }

public class ProjectAuthorizationHandler : AuthorizationHandler<ProjectMembershipRequirement, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public ProjectAuthorizationHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        ProjectMembershipRequirement requirement, 
        Guid projectId)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        // Check if user is a member or owner of the project
        var isMember = await _uow.Projects.IsUserMemberAsync(projectId, userId);
        if (isMember)
        {
            context.Succeed(requirement);
        }
    }
}
