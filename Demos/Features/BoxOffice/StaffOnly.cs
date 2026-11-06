using Featurewise;
using Microsoft.AspNetCore.Authorization;

namespace Demos.BoxOffice;

// The rule for the box office's own commands: only its staff may invoke them.
public sealed class StaffOnly<TArgs> : AuthorizationHandler<InvokeRequirement, TArgs> where TArgs : Link
{
    public const string Role = "staff";

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, InvokeRequirement requirement, TArgs link)
    {
        if (context.User.IsInRole(Role))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
