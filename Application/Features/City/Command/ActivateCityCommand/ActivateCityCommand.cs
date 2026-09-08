using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.City.Command.ActivateCityCommand
{
    public record ActivateCityCommand : IRequest<Result<int>>
    {
        public int CityId { get; set; }
    }

    public class ActivateCityCommandHandler : IRequestHandler<ActivateCityCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public ActivateCityCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<int>> Handle(ActivateCityCommand request, CancellationToken cancellationToken)
        {
            var city = await _context.Cities
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CityId == request.CityId, cancellationToken);

            if (city == null)
            {
                return Result.Failure<int>($"City with ID {request.CityId} not found");
            }

            if (city.IsActive)
            {
                return Result.Failure<int>("City is already active");
            }

            city.Activate(_userSession.UserName ?? "System");
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<int>($"Failed to activate city: {saveResult.ErrorMessage}");
            }

            return Result.Success(city.CityId);
        }
    }
}

