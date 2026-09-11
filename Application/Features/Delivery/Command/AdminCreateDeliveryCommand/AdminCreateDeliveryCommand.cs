using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Delivery.Command.AdminCreateDeliveryCommand
{
    public record AdminCreateDeliveryCommand : IRequest<Result<int>>
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string? PersonalImage { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AdminCreateDeliveryCommandHandler : IRequestHandler<AdminCreateDeliveryCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;

        public AdminCreateDeliveryCommandHandler(
            DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IUserSession userSession,
            IImageService imageService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _userSession = userSession;
            _imageService = imageService;
        }

        public async Task<Result<int>> Handle(AdminCreateDeliveryCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return Result.Failure<int>("Full name is required");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return Result.Failure<int>("Username is required");

            if (string.IsNullOrWhiteSpace(request.MobileNumber))
                return Result.Failure<int>("Mobile number is required");

            if (string.IsNullOrWhiteSpace(request.Email))
                return Result.Failure<int>("Email is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                return Result.Failure<int>("Password is required");

            if (request.CityId <= 0)
                return Result.Failure<int>("City is required");

            var cityExists = await _context.Cities
                .AnyAsync(c => c.CityId == request.CityId && c.IsActive, cancellationToken);
            if (!cityExists)
                return Result.Failure<int>("Invalid or inactive city");

            if (!await _roleManager.RoleExistsAsync(AppRoleNames.Delivery))
                return Result.Failure<int>("Delivery role is not configured");

            var userName = request.UserName.Trim();
            var mobile = request.MobileNumber.Trim();
            var email = request.Email.Trim();

            var existingByUserName = await _userManager.FindByNameAsync(userName);
            if (existingByUserName != null)
                return Result.Failure<int>("User with this username already exists");

            var existingByPhone = await _userManager.Users
                .AnyAsync(u => u.PhoneNumber == mobile, cancellationToken);
            if (existingByPhone)
                return Result.Failure<int>("User with this mobile number already exists");

            var existingDeliveryMobile = await _context.Deliveries
                .AnyAsync(d => d.MobileNumber == mobile && !d.IsDeleted, cancellationToken);
            if (existingDeliveryMobile)
                return Result.Failure<int>("Delivery with this mobile number already exists");

            var existingByEmail = await _userManager.FindByEmailAsync(email);
            if (existingByEmail != null)
                return Result.Failure<int>("User with this email already exists");

            string? personalImageUrl = null;
            if (!string.IsNullOrWhiteSpace(request.PersonalImage))
            {
                try
                {
                    personalImageUrl = _imageService.SaveBase64Image(request.PersonalImage, "deliveries");
                }
                catch (Exception ex)
                {
                    return Result.Failure<int>($"Failed to save personal image: {ex.Message}");
                }
            }

            var createdBy = _userSession.UserName ?? "Admin";

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                PhoneNumber = mobile,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Active = request.IsActive,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return Result.Failure<int>($"Failed to create user: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, AppRoleNames.Delivery);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return Result.Failure<int>($"Failed to assign role: {errors}");
            }

            var delivery = Domain.Models.Delivery.CreateByAdmin(
                user.Id,
                request.CityId,
                request.FullName,
                mobile,
                email,
                personalImageUrl,
                createdBy,
                isActive: request.IsActive);

            _context.Deliveries.Add(delivery);

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                await _userManager.DeleteAsync(user);
                return Result.Failure<int>($"Failed to save delivery: {saveResult.ErrorMessage}");
            }

            return Result.Success(delivery.DeliveryId);
        }
    }
}
