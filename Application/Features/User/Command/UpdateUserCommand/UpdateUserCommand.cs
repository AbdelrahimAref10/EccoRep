using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.User.Command.UpdateUserCommand
{
    public record UpdateUserCommand : IRequest<Result>
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public int RoleId { get; set; }
    }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
    {
        private readonly UserManager<Domain.Models.ApplicationUser> _userManager;
        private readonly RoleManager<Domain.Models.ApplicationRole> _roleManager;
        private readonly IUserSession _userSession;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateUserCommandHandler(
            UserManager<Domain.Models.ApplicationUser> userManager,
            RoleManager<Domain.Models.ApplicationRole> roleManager,
            IUserSession userSession,
            IDateTimeProvider dateTimeProvider)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userSession = userSession;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result.Failure("Email is required");
            }

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return Result.Failure("Phone number is required");
            }

            // Validate that the role exists
            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role == null)
            {
                return Result.Failure($"Role with ID '{request.RoleId}' does not exist");
            }

            // Check if username is taken by another user
            var existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null && existingUser.Id != request.UserId)
            {
                return Result.Failure("Username is already taken");
            }

            // Check if email is taken by another user
            existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != request.UserId)
            {
                return Result.Failure("Email is already taken");
            }

            // Update user properties
            user.UserName = request.UserName;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.LastModifiedBy = _userSession.UserName;
            user.LastModifiedDate = _dateTimeProvider.Now;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to update user: {errors}");
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
                if (!passwordResult.Succeeded)
                {
                    var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                    return Result.Failure($"Failed to update password: {errors}");
                }
            }

            // Update role - remove all current roles and add the new one
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return Result.Failure($"Failed to remove roles: {errors}");
                }
            }

            // Add the new role
            var addResult = await _userManager.AddToRoleAsync(user, role.Name!);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to assign role: {errors}");
            }

            return Result.Success();
        }
    }
}


