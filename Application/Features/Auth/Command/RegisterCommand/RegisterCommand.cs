using Application.Features.Auth.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Command.RegisterCommand
{
    public record RegisterCommand : IRequest<Result<RegisterResponse>>
    {
        /// <summary>AppRole enum int: Customer=1, Merchant=3, Delivery=4. SuperAdmin is not allowed.</summary>
        public int Role { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PersonalImage { get; set; }

        // Customer-only
        public string? Gender { get; set; }
        public int? CityId { get; set; }
        public int? ZoneId { get; set; }
        public string? CommercialRegisterImage { get; set; }
        public int? RegisterAs { get; set; }
        public int? VerificationBy { get; set; }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IInvitationCodeService _invitationCodeService;
        private readonly IImageService _imageService;
        private readonly RegisterCommandValidator _validator;

        public RegisterCommandHandler(
            DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IInvitationCodeService invitationCodeService,
            IImageService imageService,
            RegisterCommandValidator validator)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _invitationCodeService = invitationCodeService;
            _imageService = imageService;
            _validator = validator;
        }

        public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
                return Result.Failure<RegisterResponse>(validationResult.Error);

            if (!AppRoleNames.TryFromInt(request.Role, out var appRole))
                return Result.Failure<RegisterResponse>("Invalid role");

            if (!AppRoleNames.IsPublicRegistrationAllowed(appRole))
                return Result.Failure<RegisterResponse>("Super Admin accounts cannot be registered publicly");

            var roleName = AppRoleNames.ToRoleName(appRole);
            if (!await _roleManager.RoleExistsAsync(roleName))
                return Result.Failure<RegisterResponse>($"Role '{roleName}' is not configured");

            var existingByPhone = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.MobileNumber, cancellationToken);
            if (existingByPhone != null)
                return Result.Failure<RegisterResponse>("User with this mobile number already exists");

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
                if (existingByEmail != null)
                    return Result.Failure<RegisterResponse>("User with this email already exists");
            }

            string? personalImageUrl = null;
            if (!string.IsNullOrWhiteSpace(request.PersonalImage))
            {
                try
                {
                    personalImageUrl = _imageService.SaveBase64Image(request.PersonalImage, GetImageFolder(appRole));
                }
                catch (Exception ex)
                {
                    return Result.Failure<RegisterResponse>($"Failed to save personal image: {ex.Message}");
                }
            }

            string? commercialRegisterImageUrl = null;
            if (appRole == AppRole.Customer
                && request.RegisterAs == (int)Domain.Enums.RegisterAs.Institution
                && !string.IsNullOrWhiteSpace(request.CommercialRegisterImage))
            {
                try
                {
                    commercialRegisterImageUrl = _imageService.SaveBase64Image(request.CommercialRegisterImage, "customers");
                }
                catch (Exception ex)
                {
                    return Result.Failure<RegisterResponse>($"Failed to save commercial register image: {ex.Message}");
                }
            }

            var invitationCode = _invitationCodeService.GenerateInvitationCode();
            var userName = request.MobileNumber;

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = request.Email,
                PhoneNumber = request.MobileNumber,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                Active = true,
                CreatedBy = "System",
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return Result.Failure<RegisterResponse>($"Failed to create user: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return Result.Failure<RegisterResponse>($"Failed to assign role: {errors}");
            }

            var response = new RegisterResponse
            {
                UserId = user.Id,
                InvitationCode = invitationCode
            };

            switch (appRole)
            {
                case AppRole.Customer:
                {
                    var customer = Domain.Models.Customer.Create(
                        user.Id,
                        request.MobileNumber,
                        request.FullName,
                        request.Gender!,
                        invitationCode,
                        request.CityId!.Value,
                        request.ZoneId!.Value,
                        request.RegisterAs!.Value,
                        request.VerificationBy!.Value,
                        request.Email,
                        personalImageUrl,
                        commercialRegisterImageUrl,
                        "System");

                    _context.Customers.Add(customer);
                    var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
                    if (!saveResult.IsSuccess)
                    {
                        await _userManager.DeleteAsync(user);
                        return Result.Failure<RegisterResponse>($"Failed to save customer: {saveResult.ErrorMessage}");
                    }

                    await _invitationCodeService.SendInvitationCodeAsync(
                        request.MobileNumber,
                        request.Email,
                        request.VerificationBy!.Value,
                        invitationCode);

                    response.CustomerId = customer.CustomerId;
                    response.Message = request.VerificationBy == (int)VerificationBy.Email
                        ? "Registration successful. Please check your email for the activation code."
                        : "Registration successful. Please check your phone for the activation code.";
                    break;
                }
                case AppRole.Merchant:
                {
                    var merchant = Domain.Models.Merchant.Create(
                        user.Id,
                        request.CityId!.Value,
                        request.ZoneId!.Value,
                        request.FullName,
                        request.MobileNumber,
                        invitationCode,
                        request.Email,
                        personalImageUrl,
                        "System");

                    _context.Merchants.Add(merchant);
                    var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
                    if (!saveResult.IsSuccess)
                    {
                        await _userManager.DeleteAsync(user);
                        return Result.Failure<RegisterResponse>($"Failed to save merchant: {saveResult.ErrorMessage}");
                    }

                    await _invitationCodeService.SendInvitationCodeAsync(
                        request.MobileNumber,
                        request.Email,
                        string.IsNullOrWhiteSpace(request.Email) ? (int)VerificationBy.Phone : (int)VerificationBy.Email,
                        invitationCode);

                    response.MerchantId = merchant.MerchantId;
                    response.Message = "Registration successful. Please check your phone/email for the activation code.";
                    break;
                }
                case AppRole.Delivery:
                {
                    var delivery = Domain.Models.Delivery.Create(
                        user.Id,
                        request.CityId!.Value,
                        request.ZoneId!.Value,
                        request.FullName,
                        request.MobileNumber,
                        invitationCode,
                        request.Email,
                        personalImageUrl,
                        "System");

                    _context.Deliveries.Add(delivery);
                    var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
                    if (!saveResult.IsSuccess)
                    {
                        await _userManager.DeleteAsync(user);
                        return Result.Failure<RegisterResponse>($"Failed to save delivery: {saveResult.ErrorMessage}");
                    }

                    await _invitationCodeService.SendInvitationCodeAsync(
                        request.MobileNumber,
                        request.Email,
                        string.IsNullOrWhiteSpace(request.Email) ? (int)VerificationBy.Phone : (int)VerificationBy.Email,
                        invitationCode);

                    response.DeliveryId = delivery.DeliveryId;
                    response.Message = "Registration successful. Please check your phone/email for the activation code.";
                    break;
                }
            }

            return Result.Success(response);
        }

        private static string GetImageFolder(AppRole role) => role switch
        {
            AppRole.Customer => "customers",
            AppRole.Merchant => "merchants",
            AppRole.Delivery => "deliveries",
            _ => "users"
        };
    }
}
