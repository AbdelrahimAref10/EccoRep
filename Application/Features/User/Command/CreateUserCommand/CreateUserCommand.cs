using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.User.Command.CreateUserCommand
{
    public record CreateUserCommand : IRequest<Result<int>>
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Password { get; set; } = string.Empty;
        /// <summary>AppRole enum int: Customer=1, SuperAdmin=2, Merchant=3, Delivery=4.</summary>
        public int Role { get; set; }
    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<int>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreateUserCommandHandler(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            DatabaseContext context,
            IUserSession userSession,
            IDateTimeProvider dateTimeProvider)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _userSession = userSession;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<int>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return Result.Failure<int>("User name is required");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return Result.Failure<int>("Full name is required");

            if (string.IsNullOrWhiteSpace(request.Email))
                return Result.Failure<int>("Email is required");

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Result.Failure<int>("Phone number is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                return Result.Failure<int>("Password is required");

            if (!AppRoleNames.TryFromInt(request.Role, out var appRole))
                return Result.Failure<int>("Invalid role");

            var roleName = AppRoleNames.ToRoleName(appRole);
            if (!await _roleManager.RoleExistsAsync(roleName))
                return Result.Failure<int>($"Role '{roleName}' does not exist");

            var existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
                return Result.Failure<int>("User with this username already exists");

            existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return Result.Failure<int>("User with this email already exists");

            var existingByPhone = await _userManager.Users
                .AnyAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);
            if (existingByPhone)
                return Result.Failure<int>("User with this phone number already exists");

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Active = true,
                CreatedBy = _userSession.UserName,
                CreatedDate = _dateTimeProvider.Now,
                LastModifiedDate = _dateTimeProvider.Now
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure<int>($"Failed to create user: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return Result.Failure<int>($"Failed to assign role: {errors}");
            }

            var createdBy = _userSession.UserName ?? "System";
            var invitationCode = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

            switch (appRole)
            {
                case AppRole.SuperAdmin:
                    _context.Employees.Add(Employee.Create(user.Id, request.FullName, createdBy));
                    break;
                case AppRole.Merchant:
                    _context.Merchants.Add(Merchant.Create(
                        user.Id, request.FullName, request.PhoneNumber, invitationCode,
                        request.Email, createdBy: createdBy, isActive: true));
                    break;
                case AppRole.Delivery:
                    _context.Deliveries.Add(Domain.Models.Delivery.Create(
                        user.Id, request.FullName, request.PhoneNumber, invitationCode,
                        request.Email, createdBy: createdBy, isActive: true));
                    break;
                case AppRole.Customer:
                    return Result.Failure<int>("Use Admin Customer endpoints to create customers with full profile data");
            }

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                await _userManager.DeleteAsync(user);
                return Result.Failure<int>($"Failed to create profile: {saveResult.ErrorMessage}");
            }

            return Result.Success(user.Id);
        }
    }
}
