using CSharpFunctionalExtensions;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Roles.Command.DeleteRoleCommand
{
    public record DeleteRoleCommand : IRequest<Result<bool>>
    {
        public int RoleId { get; set; }
    }

    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<bool>>
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public DeleteRoleCommandHandler(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<Result<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role == null)
            {
                return Result.Failure<bool>($"Role with ID {request.RoleId} not found");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure<bool>($"Failed to delete role: {errors}");
            }

            return Result.Success(true);
        }
    }
}

