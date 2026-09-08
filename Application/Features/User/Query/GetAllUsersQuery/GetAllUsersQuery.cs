using Application.Common;
using Application.Features.User.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.User.Query.GetAllUsersQuery
{
    public record GetAllUsersQuery : IRequest<Result<PagedResult<UserDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public bool? Active { get; set; }
    }

    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<PagedResult<UserDto>>>
    {
        private readonly UserManager<Domain.Models.ApplicationUser> _userManager;
        private readonly RoleManager<Domain.Models.ApplicationRole> _roleManager;
        private readonly DatabaseContext _context;

        public GetAllUsersQueryHandler(
            UserManager<Domain.Models.ApplicationUser> userManager,
            RoleManager<Domain.Models.ApplicationRole> roleManager,
            DatabaseContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<Result<PagedResult<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var query = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(u => 
                    (u.UserName != null && u.UserName.ToLower().Contains(searchTerm)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
            }

            // Apply role filter
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = await _roleManager.FindByNameAsync(request.Role);
                if (role != null)
                {
                    var userIdsInRole = await _context.UserRoles
                        .Where(ur => ur.RoleId == role.Id)
                        .Select(ur => ur.UserId)
                        .ToListAsync(cancellationToken);
                    
                    query = query.Where(u => userIdsInRole.Contains(u.Id));
                }
            }

            // Apply active filter
            if (request.Active.HasValue)
            {
                query = query.Where(u => u.Active == request.Active.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    Active = user.Active,
                    Roles = roles.ToList(),
                    CreatedDate = user.CreatedDate,
                    CreatedBy = user.CreatedBy,
                    LastModifiedDate = user.LastModifiedDate,
                    LastModifiedBy = user.LastModifiedBy
                });
            }

            var result = new PagedResult<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return Result.Success(result);
        }
    }
}


