using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.City.Command.PermanentlyDeleteCityCommand
{
    public record PermanentlyDeleteCityCommand : IRequest<Result<bool>>
    {
        public int CityId { get; set; }
    }

    public class PermanentlyDeleteCityCommandHandler : IRequestHandler<PermanentlyDeleteCityCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;

        public PermanentlyDeleteCityCommandHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> Handle(PermanentlyDeleteCityCommand request, CancellationToken cancellationToken)
        {
            var city = await _context.Cities
                .Include(c => c.Customers)
                .FirstOrDefaultAsync(c => c.CityId == request.CityId, cancellationToken);

            if (city == null)
            {
                return Result.Failure<bool>($"City with ID {request.CityId} not found");
            }

            // Only allow permanent deletion of inactive cities
            if (city.IsActive)
            {
                return Result.Failure<bool>("Cannot permanently delete an active city. Please deactivate it first.");
            }

            // Check if city has customers
            if (city.Customers.Any())
            {
                return Result.Failure<bool>("Cannot permanently delete city that has customers. Please reassign customers to another city first.");
            }

            _context.Cities.Remove(city);
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to permanently delete city: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}

