using Application.Features.Auth.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Command.ResendActivationCodeCommand
{
    public record ResendActivationCodeCommand : IRequest<Result<MessageResponse>>
    {
        public int Role { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class ResendActivationCodeCommandHandler : IRequestHandler<ResendActivationCodeCommand, Result<MessageResponse>>
    {
        private readonly DatabaseContext _context;
        private readonly IInvitationCodeService _invitationCodeService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ResendActivationCodeCommandHandler(
            DatabaseContext context,
            IInvitationCodeService invitationCodeService,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _invitationCodeService = invitationCodeService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<MessageResponse>> Handle(ResendActivationCodeCommand request, CancellationToken cancellationToken)
        {
            if (!AppRoleNames.TryFromInt(request.Role, out var appRole))
                return Result.Failure<MessageResponse>("Invalid role");

            if (string.IsNullOrWhiteSpace(request.MobileNumber))
                return Result.Failure<MessageResponse>("Mobile number is required");

            var code = _invitationCodeService.GenerateInvitationCode();
            int verificationBy = string.IsNullOrWhiteSpace(request.Email)
                ? (int)VerificationBy.Phone
                : (int)VerificationBy.Email;
            string? email = request.Email;

            switch (appRole)
            {
                case AppRole.Customer:
                {
                    var customer = await _context.Customers.AsTracking()
                        .FirstOrDefaultAsync(c => c.MobileNumber == request.MobileNumber, cancellationToken);
                    if (customer == null)
                        return Result.Failure<MessageResponse>("Customer not found");

                    if (customer.State == CustomerState.Active)
                        return Result.Failure<MessageResponse>("Account is already active");

                    customer.RegenerateInvitationCode(code, _dateTimeProvider);
                    verificationBy = customer.VerificationBy;
                    email = customer.Email;
                    break;
                }
                case AppRole.Merchant:
                {
                    var merchant = await _context.Merchants.AsTracking()
                        .FirstOrDefaultAsync(m => m.MobileNumber == request.MobileNumber, cancellationToken);
                    if (merchant == null)
                        return Result.Failure<MessageResponse>("Merchant not found");

                    if (merchant.IsActive)
                        return Result.Failure<MessageResponse>("Account is already active");

                    merchant.RegenerateInvitationCode(code, _dateTimeProvider);
                    email = merchant.Email;
                    break;
                }
                case AppRole.Delivery:
                {
                    var delivery = await _context.Deliveries.AsTracking()
                        .FirstOrDefaultAsync(d => d.MobileNumber == request.MobileNumber, cancellationToken);
                    if (delivery == null)
                        return Result.Failure<MessageResponse>("Delivery not found");

                    if (delivery.IsActive)
                        return Result.Failure<MessageResponse>("Account is already active");

                    delivery.RegenerateInvitationCode(code, _dateTimeProvider);
                    email = delivery.Email;
                    break;
                }
                default:
                    return Result.Failure<MessageResponse>("Unsupported role");
            }

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
                return Result.Failure<MessageResponse>($"Failed to resend code: {saveResult.ErrorMessage}");

            await _invitationCodeService.SendInvitationCodeAsync(
                request.MobileNumber,
                email,
                verificationBy,
                code);

            return Result.Success(new MessageResponse
            {
                Message = "Activation code has been resent successfully."
            });
        }
    }
}
