using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Command.ActivateAccountCommand
{
    public record ActivateAccountCommand : IRequest<Result<bool>>
    {
        public int Role { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string InvitationCode { get; set; } = string.Empty;
    }

    public class ActivateAccountCommandHandler : IRequestHandler<ActivateAccountCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ActivateAccountCommandHandler(
            DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _userManager = userManager;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<bool>> Handle(ActivateAccountCommand request, CancellationToken cancellationToken)
        {
            if (!AppRoleNames.TryFromInt(request.Role, out var appRole))
                return Result.Failure<bool>("Invalid role");

            if (appRole == AppRole.SuperAdmin)
                return Result.Failure<bool>("Super Admin activation is not supported via this endpoint");

            if (string.IsNullOrWhiteSpace(request.MobileNumber))
                return Result.Failure<bool>("Mobile number is required");

            if (string.IsNullOrWhiteSpace(request.InvitationCode))
                return Result.Failure<bool>("Invitation code is required");

            switch (appRole)
            {
                case AppRole.Customer:
                {
                    var customer = await _context.Customers.AsTracking()
                        .FirstOrDefaultAsync(c => c.MobileNumber == request.MobileNumber, cancellationToken);
                    if (customer == null)
                        return Result.Failure<bool>("Customer not found");

                    if (!customer.ValidateInvitationCode(request.InvitationCode, _dateTimeProvider))
                        return Result.Failure<bool>("Invalid or expired invitation code");

                    customer.Activate("System");
                    break;
                }
                case AppRole.Merchant:
                {
                    var merchant = await _context.Merchants.AsTracking()
                        .FirstOrDefaultAsync(m => m.MobileNumber == request.MobileNumber, cancellationToken);
                    if (merchant == null)
                        return Result.Failure<bool>("Merchant not found");

                    if (!merchant.ValidateInvitationCode(request.InvitationCode, _dateTimeProvider))
                        return Result.Failure<bool>("Invalid or expired invitation code");

                    merchant.Activate("System");
                    break;
                }
                case AppRole.Delivery:
                {
                    var delivery = await _context.Deliveries.AsTracking()
                        .FirstOrDefaultAsync(d => d.MobileNumber == request.MobileNumber, cancellationToken);
                    if (delivery == null)
                        return Result.Failure<bool>("Delivery not found");

                    if (!delivery.ValidateInvitationCode(request.InvitationCode, _dateTimeProvider))
                        return Result.Failure<bool>("Invalid or expired invitation code");

                    delivery.Activate("System");
                    break;
                }
                default:
                    return Result.Failure<bool>("Unsupported role");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.MobileNumber, cancellationToken);
            if (user != null)
            {
                user.PhoneNumberConfirmed = true;
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
                return Result.Failure<bool>($"Failed to activate account: {saveResult.ErrorMessage}");

            return Result.Success(true);
        }
    }
}
