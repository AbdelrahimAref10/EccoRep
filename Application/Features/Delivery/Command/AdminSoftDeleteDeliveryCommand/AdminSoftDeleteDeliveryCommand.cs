using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Delivery.Command.AdminSoftDeleteDeliveryCommand
{
    public record AdminSoftDeleteDeliveryCommand : IRequest<Result<bool>>
    {
        public int DeliveryId { get; set; }
    }

    public class AdminSoftDeleteDeliveryCommandHandler : IRequestHandler<AdminSoftDeleteDeliveryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserSession _userSession;

        public AdminSoftDeleteDeliveryCommandHandler(
            DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            IUserSession userSession)
        {
            _context = context;
            _userManager = userManager;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(AdminSoftDeleteDeliveryCommand request, CancellationToken cancellationToken)
        {
            if (request.DeliveryId <= 0)
                return Result.Failure<bool>("DeliveryId is required");

            var delivery = await _context.Deliveries
                .AsTracking()
                .FirstOrDefaultAsync(d => d.DeliveryId == request.DeliveryId && !d.IsDeleted, cancellationToken);
            if (delivery == null)
                return Result.Failure<bool>("Delivery not found");

            var modifiedBy = _userSession.UserName ?? "Admin";
            delivery.SoftDelete(modifiedBy);

            var user = await _userManager.FindByIdAsync(delivery.UserId.ToString());
            if (user != null)
            {
                user.Active = false;
                user.LastModifiedBy = modifiedBy;
                user.LastModifiedDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
                return Result.Failure<bool>($"Failed to delete delivery: {saveResult.ErrorMessage}");

            return Result.Success(true);
        }
    }
}
