using Application.Features.Auth.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Command.ForgetPasswordCommand
{
    public record ForgetPasswordCommand : IRequest<Result<MessageResponse>>
    {
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
    }

    public class ForgetPasswordCommandHandler : IRequestHandler<ForgetPasswordCommand, Result<MessageResponse>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IInvitationCodeService _invitationCodeService;
        private readonly ForgetPasswordCommandValidator _validator;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ForgetPasswordCommandHandler(
            UserManager<ApplicationUser> userManager,
            IInvitationCodeService invitationCodeService,
            ForgetPasswordCommandValidator validator,
            IDateTimeProvider dateTimeProvider)
        {
            _userManager = userManager;
            _invitationCodeService = invitationCodeService;
            _validator = validator;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<MessageResponse>> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
                return Result.Failure<MessageResponse>(validationResult.Error);

            var user = await FindUserAsync(request, cancellationToken);
            if (user == null)
                return Result.Failure<MessageResponse>("User not found");

            if (!user.Active)
                return Result.Failure<MessageResponse>("Account is not active");

            var code = _invitationCodeService.GenerateInvitationCode();
            user.SetPasswordResetCode(code, _dateTimeProvider, expiryMinutes: 15);
            await _userManager.UpdateAsync(user);

            var sendToEmail = !string.IsNullOrWhiteSpace(request.Email)
                || (string.IsNullOrWhiteSpace(request.MobileNumber) && !string.IsNullOrWhiteSpace(user.Email));

            if (sendToEmail && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _invitationCodeService.SendInvitationCodeAsync(
                    user.PhoneNumber ?? string.Empty,
                    user.Email,
                    (int)VerificationBy.Email,
                    code);

                return Result.Success(new MessageResponse
                {
                    Message = "Password reset code has been sent to your email."
                });
            }

            await _invitationCodeService.SendInvitationCodeAsync(
                user.PhoneNumber ?? request.MobileNumber ?? string.Empty,
                user.Email,
                (int)VerificationBy.Phone,
                code);

            return Result.Success(new MessageResponse
            {
                Message = "Password reset code has been sent to your phone."
            });
        }

        private async Task<ApplicationUser?> FindUserAsync(ForgetPasswordCommand request, CancellationToken cancellationToken)
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
