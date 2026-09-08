using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.City.Command.DeactivateCityCommand
{
    public record DeactivateCityCommand : IRequest<Result<bool>>
    {
        public int CityId { get; set; }
    }

    public class DeactivateCityCommandHandler : IRequestHandler<DeactivateCityCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public DeactivateCityCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(DeactivateCityCommand request, CancellationToken cancellationToken)
        {
            var city = await _context.Cities
                .AsTracking()
                .Include(c => c.Customers)
                .FirstOrDefaultAsync(c => c.CityId == request.CityId, cancellationToken);

            if (city == null)
            {
                return Result.Failure<bool>($"City with ID {request.CityId} not found");
            }

            if (!city.IsActive)
            {
                return Result.Failure<bool>("City is already inactive");
            }

            // Check if city has customers
            if (city.Customers.Any())
            {
                return Result.Failure<bool>("Cannot deactivate city that has customers. Please reassign customers to another city first.");
            }

            city.Deactivate(_userSession.UserName ?? "System");
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to deactivate city: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}

