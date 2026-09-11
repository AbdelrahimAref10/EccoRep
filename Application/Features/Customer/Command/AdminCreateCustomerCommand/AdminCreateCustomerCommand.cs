using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Customer.Command.AdminCreateCustomerCommand
{
    /// <summary>
    /// Admin creates a customer with full profile. Active immediately — no OTP/invitation.
    /// VerificationBy is preference only (contact channel), not an activation gate.
    /// </summary>
    public record AdminCreateCustomerCommand : IRequest<Result<int>>
    {
        public string MobileNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string? PersonalImage { get; set; }
        public string? Email { get; set; }
        public string? CommercialRegisterImage { get; set; }
        public int RegisterAs { get; set; }
        public int VerificationBy { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public class AdminCreateCustomerCommandHandler : IRequestHandler<AdminCreateCustomerCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly AdminCreateCustomerCommandValidator _validator;

        public AdminCreateCustomerCommandHandler(
            DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IUserSession userSession,
            IImageService imageService,
            AdminCreateCustomerCommandValidator validator)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _userSession = userSession;
            _imageService = imageService;
            _validator = validator;
        }

        public async Task<Result<int>> Handle(AdminCreateCustomerCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
                return Result.Failure<int>(validationResult.Error);

            if (!await _roleManager.RoleExistsAsync(AppRoleNames.Customer))
                return Result.Failure<int>("Customer role is not configured");

            var mobile = request.MobileNumber.Trim();
            var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

            var existingByPhone = await _userManager.Users
                .AnyAsync(u => u.PhoneNumber == mobile, cancellationToken);
            if (existingByPhone)
                return Result.Failure<int>("User with this mobile number already exists");

            var existingByUserName = await _userManager.FindByNameAsync(mobile);
            if (existingByUserName != null)
                return Result.Failure<int>("User with this username already exists");

            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingByEmail = await _userManager.FindByEmailAsync(email);
                if (existingByEmail != null)
                    return Result.Failure<int>("User with this email already exists");
            }

            string? personalImageUrl = null;
            if (!string.IsNullOrWhiteSpace(request.PersonalImage))
            {
                try
                {
                    personalImageUrl = _imageService.SaveBase64Image(request.PersonalImage, "customers");
                }
                catch (Exception ex)
                {
                    return Result.Failure<int>($"Failed to save personal image: {ex.Message}");
                }
            }

            string? commercialRegisterImageUrl = null;
            if (request.RegisterAs == (int)Domain.Enums.RegisterAs.Institution
                && !string.IsNullOrWhiteSpace(request.CommercialRegisterImage))
            {
                try
                {
                    commercialRegisterImageUrl = _imageService.SaveBase64Image(request.CommercialRegisterImage, "customers");
                }
                catch (Exception ex)
                {
                    return Result.Failure<int>($"Failed to save commercial register image: {ex.Message}");
                }
            }

            var createdBy = _userSession.UserName ?? "Admin";

            var user = new ApplicationUser
            {
                UserName = mobile,
                Email = email,
                PhoneNumber = mobile,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Active = true,
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

            var roleResult = await _userManager.AddToRoleAsync(user, AppRoleNames.Customer);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return Result.Failure<int>($"Failed to assign role: {errors}");
            }

            var customer = Domain.Models.Customer.CreateByAdmin(
                user.Id,
                mobile,
                request.FullName,
                request.Gender,
                request.CityId,
                request.RegisterAs,
                request.VerificationBy,
                email,
                personalImageUrl,
                commercialRegisterImageUrl,
                createdBy);

            _context.Customers.Add(customer);

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                await _userManager.DeleteAsync(user);
                return Result.Failure<int>($"Failed to save customer: {saveResult.ErrorMessage}");
            }

            return Result.Success(customer.CustomerId);
        }
    }
}
