using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Delivery.Command.AdminUpdateDeliveryCommand
{
    public record AdminUpdateDeliveryCommand : IRequest<Result<bool>>
    {
        public int DeliveryId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string? PersonalImage { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Password { get; set; }
    }

    public class AdminUpdateDeliveryCommandHandler : IRequestHandler<AdminUpdateDeliveryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;

        public AdminUpdateDeliveryCommandHandler(
            DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            IUserSession userSession,
            IImageService imageService)
        {
            _context = context;
            _userManager = userManager;
            _userSession = userSession;
            _imageService = imageService;
        }

        public async Task<Result<bool>> Handle(AdminUpdateDeliveryCommand request, CancellationToken cancellationToken)
        {
            if (request.DeliveryId <= 0)
                return Result.Failure<bool>("DeliveryId is required");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return Result.Failure<bool>("Full name is required");

            if (string.IsNullOrWhiteSpace(request.UserName))
                return Result.Failure<bool>("Username is required");

            if (string.IsNullOrWhiteSpace(request.Email))
                return Result.Failure<bool>("Email is required");

            if (request.CityId <= 0)
                return Result.Failure<bool>("City is required");

            var cityExists = await _context.Cities
                .AnyAsync(c => c.CityId == request.CityId && c.IsActive, cancellationToken);
            if (!cityExists)
                return Result.Failure<bool>("Invalid or inactive city");

            var delivery = await _context.Deliveries
                .AsTracking()
                .FirstOrDefaultAsync(d => d.DeliveryId == request.DeliveryId && !d.IsDeleted, cancellationToken);
            if (delivery == null)
                return Result.Failure<bool>("Delivery not found");

            var user = await _userManager.FindByIdAsync(delivery.UserId.ToString());
            if (user == null)
                return Result.Failure<bool>("Linked user not found");

            var userName = request.UserName.Trim();
            var existingByUserName = await _userManager.FindByNameAsync(userName);
            if (existingByUserName != null && existingByUserName.Id != user.Id)
                return Result.Failure<bool>("User with this username already exists");

            var email = request.Email.Trim();
            var existingByEmail = await _userManager.FindByEmailAsync(email);
            if (existingByEmail != null && existingByEmail.Id != user.Id)
                return Result.Failure<bool>("User with this email already exists");

            string? personalImageUrl = delivery.PersonalImage;
            if (!string.IsNullOrWhiteSpace(request.PersonalImage))
            {
                if (request.PersonalImage.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        personalImageUrl = _imageService.SaveBase64Image(request.PersonalImage, "deliveries");
                    }
                    catch (Exception ex)
                    {
                        return Result.Failure<bool>($"Failed to save personal image: {ex.Message}");
                    }
                }
                else
                {
                    personalImageUrl = request.PersonalImage;
                }
            }

            var modifiedBy = _userSession.UserName ?? "Admin";
            delivery.UpdateProfile(
                request.FullName,
                email,
                personalImageUrl,
                modifiedBy,
                isActive: request.IsActive,
                cityId: request.CityId);

            if (!string.Equals(user.UserName, userName, StringComparison.Ordinal))
            {
                var setUserName = await _userManager.SetUserNameAsync(user, userName);
                if (!setUserName.Succeeded)
                {
                    var errors = string.Join(", ", setUserName.Errors.Select(e => e.Description));
                    return Result.Failure<bool>($"Failed to update username: {errors}");
                }
            }

            user.Email = email;
            user.Active = request.IsActive;
            user.LastModifiedBy = modifiedBy;
            user.LastModifiedDate = DateTime.UtcNow;

            var updateUser = await _userManager.UpdateAsync(user);
            if (!updateUser.Succeeded)
            {
                var errors = string.Join(", ", updateUser.Errors.Select(e => e.Description));
                return Result.Failure<bool>($"Failed to update user: {errors}");
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
                if (!passwordResult.Succeeded)
                {
                    var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                    return Result.Failure<bool>($"Failed to update password: {errors}");
                }
            }

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
                return Result.Failure<bool>($"Failed to update delivery: {saveResult.ErrorMessage}");

            return Result.Success(true);
        }
    }
}
