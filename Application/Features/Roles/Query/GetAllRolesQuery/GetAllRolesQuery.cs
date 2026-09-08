using Application.Features.Roles.DTOs;
using CSharpFunctionalExtensions;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Roles.Query.GetAllRolesQuery
{
    public record GetAllRolesQuery : IRequest<Result<List<RoleDto>>>;

    public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, Result<List<RoleDto>>>
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public GetAllRolesQueryHandler(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<Result<List<RoleDto>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _roleManager.Roles
                .Select(r => new RoleDto
                {
                    RoleId = r.Id,
                    RoleName = r.Name!,
                    CreatedDate = r.CreatedDate,
                    LastModifiedDate = r.LastModifiedDate
                })
                .ToListAsync(cancellationToken);

            return Result.Success(roles);
        }
    }
}

