using Application.Features.AdminHome.Common;
using Application.Features.AdminHome.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminHome.Query.GetHomeOrderPipelineQuery
{
    public record GetHomeOrderPipelineQuery : IRequest<Result<HomeOrderPipelineDto>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? CityId { get; set; }
    }

    public class GetHomeOrderPipelineQueryHandler : IRequestHandler<GetHomeOrderPipelineQuery, Result<HomeOrderPipelineDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetHomeOrderPipelineQueryHandler(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<HomeOrderPipelineDto>> Handle(GetHomeOrderPipelineQuery request, CancellationToken cancellationToken)
        {
            var now = _dateTimeProvider.Now;
            var (from, to) = AdminHomeDateRange.Resolve(request.From, request.To, now);

            var query = _context.Orders.AsNoTracking().AsQueryable();
            if (request.CityId.HasValue)
            {
                query = query.Where(o => o.CityId == request.CityId.Value);
            }

            var inRange = query.Where(o => o.CreatedDate >= from && o.CreatedDate <= to);

            var byState = await inRange
                .GroupBy(o => o.OrderState)
                .Select(g => new HomeOrderStateBucketDto
                {
                    State = (int)g.Key,
                    StateName = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            // Ensure all states appear for charts
            foreach (OrderState state in Enum.GetValues(typeof(OrderState)))
            {
                if (byState.All(x => x.State != (int)state))
                {
                    byState.Add(new HomeOrderStateBucketDto
                    {
                        State = (int)state,
                        StateName = state.ToString(),
                        Count = 0
                    });
                }
            }

            byState = byState.OrderBy(x => x.State).ToList();

            var dto = new HomeOrderPipelineDto
            {
                ByState = byState,
                CreatedInRange = byState.Sum(x => x.Count),
                CompletedInRange = byState.FirstOrDefault(x => x.State == (int)OrderState.Completed)?.Count ?? 0,
                UrgentInRange = await inRange.CountAsync(o => o.IsUrgent, cancellationToken),
                ActiveRentals = await query.CountAsync(o =>
                    o.OrderState == OrderState.Confirmed ||
                    o.OrderState == OrderState.OnWay ||
                    o.OrderState == OrderState.CustomerReceived, cancellationToken),
                PendingOrders = await query.CountAsync(o => o.OrderState == OrderState.Pending, cancellationToken),
                From = from,
                To = to
            };

            return Result.Success(dto);
        }
    }
}
