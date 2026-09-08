using CSharpFunctionalExtensions;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Roles.Command.AssignRoleToUserCommand
{
    public record AssignRoleToUserCommand : IRequest<Result<bool>>
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }

    public class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand, Result<bool>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AssignRoleToUserCommandHandler(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<bool>> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return Result.Failure<bool>($"User with ID {request.UserId} not found");
            }

            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role == null)
            {
                return Result.Failure<bool>($"Role with ID {request.RoleId} not found");
            }

            var isInRole = await _userManager.IsInRoleAsync(user, role.Name!);
            if (isInRole)
            {
                return Result.Failure<bool>($"User is already assigned to role '{role.Name}'");
            }

            var result = await _userManager.AddToRoleAsync(user, role.Name!);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure<bool>($"Failed to assign role: {errors}");
            }

            return Result.Success(true);
        }
    }
}

