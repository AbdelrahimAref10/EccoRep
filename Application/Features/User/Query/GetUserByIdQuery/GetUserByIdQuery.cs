using Application.Features.User.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.User.Query.GetUserByIdQuery
{
    public record GetUserByIdQuery(int UserId) : IRequest<Result<UserDto>>;

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
    {
        private readonly UserManager<Domain.Models.ApplicationUser> _userManager;
        private readonly DatabaseContext _context;

        public GetUserByIdQueryHandler(
            UserManager<Domain.Models.ApplicationUser> userManager,
            DatabaseContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
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
                IsActive = user.Active
            };

            return Result.Success(userDto);
        }
    }
}










