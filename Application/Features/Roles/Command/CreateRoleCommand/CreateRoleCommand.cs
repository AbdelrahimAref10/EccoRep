using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Roles.Command.CreateRoleCommand
{
    public record CreateRoleCommand : IRequest<Result<int>>
    {
        public string RoleName { get; set; } = string.Empty;
    }

    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<int>>
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreateRoleCommandHandler(RoleManager<ApplicationRole> roleManager, IDateTimeProvider dateTimeProvider)
        {
            _roleManager = roleManager;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<int>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                return Result.Failure<int>("Role name cannot be empty");
            }

            var roleExists = await _roleManager.RoleExistsAsync(request.RoleName);
            if (roleExists)
            {
                return Result.Failure<int>($"Role '{request.RoleName}' already exists");
            }

            var role = new ApplicationRole
            {
                Name = request.RoleName,
                NormalizedName = request.RoleName.ToUpperInvariant(),
                CreatedDate = _dateTimeProvider.Now
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure<int>($"Failed to create role: {errors}");
            }

            return Result.Success(role.Id);
        }
    }
}

