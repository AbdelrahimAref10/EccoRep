using Application.Features.Roles.DTOs;
using CSharpFunctionalExtensions;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Roles.Query.GetRoleByIdQuery
{
    public record GetRoleByIdQuery : IRequest<Result<RoleDto>>
    {
        public int RoleId { get; set; }
    }

    public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleDto>>
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public GetRoleByIdQueryHandler(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<Result<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role == null)
            {
                return Result.Failure<RoleDto>($"Role with ID {request.RoleId} not found");
            }

            var roleDto = new RoleDto
            {
                RoleId = role.Id,
                RoleName = role.Name!,
                CreatedDate = role.CreatedDate,
                LastModifiedDate = role.LastModifiedDate
            };

            return Result.Success(roleDto);
        }
    }
}

