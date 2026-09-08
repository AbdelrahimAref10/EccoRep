using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Customer.Command.DeleteCustomerAccountCommand
{
    public record DeleteCustomerAccountCommand : IRequest<Result<bool>>;

    public class DeleteCustomerAccountCommandHandler : IRequestHandler<DeleteCustomerAccountCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserSession _userSession;

        public DeleteCustomerAccountCommandHandler(
            DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            IUserSession userSession)
        {
            _context = context;
            _userManager = userManager;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(DeleteCustomerAccountCommand request, CancellationToken cancellationToken)
        {
            if (_userSession.UserId <= 0)
                return Result.Failure<bool>("Customer not authenticated");

            var customer = await _context.Customers
                .AsTracking()
                .Include(c => c.CustomerLocation)
                .FirstOrDefaultAsync(c => c.UserId == _userSession.UserId, cancellationToken);

            if (customer == null)
                return Result.Failure<bool>("Customer not found");

            var hasActiveOrders = await _context.Orders
                .AnyAsync(o => o.CustomerId == customer.CustomerId &&
                             (o.OrderState == Domain.Enums.OrderState.Pending ||
                              o.OrderState == Domain.Enums.OrderState.Confirmed ||
                              o.OrderState == Domain.Enums.OrderState.OnWay),
                         cancellationToken);

            if (hasActiveOrders)
                return Result.Failure<bool>("Cannot delete account. You have active orders. Please complete or cancel them first.");

            if (customer.CustomerLocation != null)
                _context.CustomerLocations.Remove(customer.CustomerLocation);

            _context.Customers.Remove(customer);

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
                return Result.Failure<bool>($"Failed to delete customer account: {saveResult.ErrorMessage}");

            var user = await _userManager.FindByIdAsync(_userSession.UserId.ToString());
            if (user != null)
                await _userManager.DeleteAsync(user);

            return Result.Success(true);
        }
    }
}
