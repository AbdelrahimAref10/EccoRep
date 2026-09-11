using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Merchant.Command.AdminSoftDeleteMerchantCommand
{
    public record AdminSoftDeleteMerchantCommand : IRequest<Result<bool>>
    {
        public int MerchantId { get; set; }
    }

    public class AdminSoftDeleteMerchantCommandHandler : IRequestHandler<AdminSoftDeleteMerchantCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserSession _userSession;

        public AdminSoftDeleteMerchantCommandHandler(
            DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            IUserSession userSession)
        {
            _context = context;
            _userManager = userManager;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(AdminSoftDeleteMerchantCommand request, CancellationToken cancellationToken)
        {
            if (request.MerchantId <= 0)
                return Result.Failure<bool>("MerchantId is required");

            var merchant = await _context.Merchants
                .AsTracking()
                .FirstOrDefaultAsync(m => m.MerchantId == request.MerchantId && !m.IsDeleted, cancellationToken);
            if (merchant == null)
                return Result.Failure<bool>("Merchant not found");

            var modifiedBy = _userSession.UserName ?? "Admin";
            merchant.SoftDelete(modifiedBy);

            var user = await _userManager.FindByIdAsync(merchant.UserId.ToString());
            if (user != null)
            {
                user.Active = false;
                user.LastModifiedBy = modifiedBy;
                user.LastModifiedDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
                return Result.Failure<bool>($"Failed to delete merchant: {saveResult.ErrorMessage}");

            return Result.Success(true);
        }
    }
}
