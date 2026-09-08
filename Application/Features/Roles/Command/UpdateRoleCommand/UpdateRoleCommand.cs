using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Roles.Command.UpdateRoleCommand
{
    public record UpdateRoleCommand : IRequest<Result<bool>>
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<bool>>
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateRoleCommandHandler(RoleManager<ApplicationRole> roleManager, IDateTimeProvider dateTimeProvider)
        {
            _roleManager = roleManager;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<bool>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                return Result.Failure<bool>("Role name cannot be empty");
            }

            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role == null)
            {
                return Result.Failure<bool>($"Role with ID {request.RoleId} not found");
            }

            // Check if another role with the same name exists
            var existingRole = await _roleManager.FindByNameAsync(request.RoleName);
            if (existingRole != null && existingRole.Id != request.RoleId)
            {
                return Result.Failure<bool>($"Role '{request.RoleName}' already exists");
            }

            role.Name = request.RoleName;
            role.NormalizedName = request.RoleName.ToUpperInvariant();
            role.LastModifiedDate = _dateTimeProvider.Now;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure<bool>($"Failed to update role: {errors}");
            }

            return Result.Success(true);
        }
    }
}

