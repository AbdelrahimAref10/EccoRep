using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Zone.Command.UpdateZoneDeliveryRatesCommand
{
    public record ZoneDeliveryRateItem
    {
        public int ToZoneId { get; set; }
        public decimal Fee { get; set; }
    }

    public record UpdateZoneDeliveryRatesCommand : IRequest<Result<bool>>
    {
        public int FromZoneId { get; set; }
        public List<ZoneDeliveryRateItem> Rates { get; set; } = new();
    }

    public class UpdateZoneDeliveryRatesCommandHandler : IRequestHandler<UpdateZoneDeliveryRatesCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public UpdateZoneDeliveryRatesCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(UpdateZoneDeliveryRatesCommand request, CancellationToken cancellationToken)
        {
            if (request.FromZoneId <= 0)
                return Result.Failure<bool>("From zone is required");

            var fromZone = await _context.Zones.AsTracking()
                .FirstOrDefaultAsync(z => z.ZoneId == request.FromZoneId, cancellationToken);

            if (fromZone == null)
                return Result.Failure<bool>("Zone not found");

            var groupZoneIds = await _context.Zones.AsNoTracking()
                .Where(z => z.ZoneGroupId == fromZone.ZoneGroupId)
                .Select(z => z.ZoneId)
                .ToListAsync(cancellationToken);

            if (request.Rates.Any(r => r.Fee < 0))
                return Result.Failure<bool>("Fee cannot be negative");

            if (request.Rates.Any(r => !groupZoneIds.Contains(r.ToZoneId)))
                return Result.Failure<bool>("All target zones must belong to the same group");

            var actor = _userSession.UserName ?? "Admin";
            var existing = await _context.ZoneDeliveryRates.AsTracking()
                .Where(r => r.FromZoneId == request.FromZoneId)
                .ToListAsync(cancellationToken);

            foreach (var item in request.Rates)
            {
                var row = existing.FirstOrDefault(r => r.ToZoneId == item.ToZoneId);
                if (row == null)
                {
                    await _context.ZoneDeliveryRates.AddAsync(
                        ZoneDeliveryRate.Create(fromZone.ZoneGroupId, request.FromZoneId, item.ToZoneId, item.Fee, actor),
                        cancellationToken);
                }
                else
                {
                    row.UpdateFee(item.Fee, actor);
                }
            }

            var save = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!save.IsSuccess)
                return Result.Failure<bool>(save.ErrorMessage ?? "Failed to save rates");

            return Result.Success(true);
        }
    }
}
