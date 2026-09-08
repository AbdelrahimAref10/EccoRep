using Application.Features.Auth.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Command.ResetPasswordCommand
{
    public record ResetPasswordCommand : IRequest<Result<MessageResponse>>
    {
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string ResetCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<MessageResponse>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ResetPasswordCommandValidator _validator;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ResetPasswordCommandHandler(
            UserManager<ApplicationUser> userManager,
            ResetPasswordCommandValidator validator,
            IDateTimeProvider dateTimeProvider)
        {
            _userManager = userManager;
            _validator = validator;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<MessageResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
                return Result.Failure<MessageResponse>(validationResult.Error);

            var user = await FindUserAsync(request, cancellationToken);
            if (user == null)
                return Result.Failure<MessageResponse>("User not found");

            if (!user.ValidatePasswordResetCode(request.ResetCode, _dateTimeProvider))
                return Result.Failure<MessageResponse>("Invalid or expired reset code. Please request a new code.");

            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                return Result.Failure<MessageResponse>($"Failed to reset password: {errors}");
            }

            var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return Result.Failure<MessageResponse>($"Failed to reset password: {errors}");
            }

            user.ClearPasswordResetCode(_dateTimeProvider);
            await _userManager.UpdateAsync(user);

            return Result.Success(new MessageResponse
            {
                Message = "Password has been reset successfully. You can now login with your new password."
            });
        }

        private async Task<ApplicationUser?> FindUserAsync(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                return await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.MobileNumber, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
                return await _userManager.FindByEmailAsync(request.Email);

            return null;
        }
    }
}
