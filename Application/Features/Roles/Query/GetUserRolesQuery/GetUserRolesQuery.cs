using Application.Features.Roles.DTOs;
using CSharpFunctionalExtensions;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Roles.Query.GetUserRolesQuery
{
    public record GetUserRolesQuery : IRequest<Result<List<RoleDto>>>
    {
        public int UserId { get; set; }
    }

    public class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, Result<List<RoleDto>>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public GetUserRolesQueryHandler(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<List<RoleDto>>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return Result.Failure<List<RoleDto>>($"User with ID {request.UserId} not found");
            }

            var roleNames = await _userManager.GetRolesAsync(user);
            var roles = new List<RoleDto>();

            foreach (var roleName in roleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    roles.Add(new RoleDto
                    {
                        RoleId = role.Id,
                        RoleName = role.Name!,
                        CreatedDate = role.CreatedDate,
                        LastModifiedDate = role.LastModifiedDate
                    });
                }
            }

            return Result.Success(roles);
        }
    }
}

