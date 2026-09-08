using Application.Features.User.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.User.Query.GetCurrentUserQuery
{
    public record GetCurrentUserQuery : IRequest<Result<UserDto>>;

    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
    {
        private readonly UserManager<Domain.Models.ApplicationUser> _userManager;
        private readonly IUserSession _userSession;

        public GetCurrentUserQueryHandler(
            UserManager<Domain.Models.ApplicationUser> userManager,
            IUserSession userSession)
        {
            _userManager = userManager;
            _userSession = userSession;
        }

        public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var userId = _userSession.UserId;
            if (userId <= 0)
            {
                return Result.Failure<UserDto>("User not authenticated");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Failure<UserDto>("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
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
                Roles = roles.ToList(),
                CreatedDate = user.CreatedDate,
                CreatedBy = user.CreatedBy,
                LastModifiedDate = user.LastModifiedDate,
                LastModifiedBy = user.LastModifiedBy,
                IsActive = user.Active,
                Active = user.Active
            };

            return Result.Success(userDto);
        }
    }
}

