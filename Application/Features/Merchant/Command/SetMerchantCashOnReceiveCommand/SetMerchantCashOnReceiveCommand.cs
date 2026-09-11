using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Merchant.Command.SetMerchantCashOnReceiveCommand
{
    public record SetMerchantCashOnReceiveCommand : IRequest<Result<bool>>
    {
        public int MerchantId { get; set; }
        public bool CashOnReceive { get; set; }
    }

    public class SetMerchantCashOnReceiveCommandHandler
        : IRequestHandler<SetMerchantCashOnReceiveCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public SetMerchantCashOnReceiveCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(
            SetMerchantCashOnReceiveCommand request,
            CancellationToken cancellationToken)
        {
            if (!_userSession.Roles.Contains(AppRoleNames.SuperAdmin))
                return Result.Failure<bool>("Only admin can update merchant cash-on-receive setting");

            var merchant = await _context.Merchants
                .AsTracking()
                .FirstOrDefaultAsync(m => m.MerchantId == request.MerchantId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<bool>($"Merchant with ID {request.MerchantId} not found");

            merchant.SetCashOnReceive(request.CashOnReceive, _userSession.UserName ?? "System");
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(true);
        }
    }
}
