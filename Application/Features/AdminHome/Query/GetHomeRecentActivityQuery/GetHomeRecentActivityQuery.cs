using Application.Features.AdminHome.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.AdminHome.Query.GetHomeRecentActivityQuery
{
    public record GetHomeRecentActivityQuery : IRequest<Result<HomeRecentActivityDto>>
    {
        public int Count { get; set; } = 10;
        public int? CityId { get; set; }
    }

    public class GetHomeRecentActivityQueryHandler : IRequestHandler<GetHomeRecentActivityQuery, Result<HomeRecentActivityDto>>
    {
        private readonly DatabaseContext _context;

        public GetHomeRecentActivityQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<HomeRecentActivityDto>> Handle(GetHomeRecentActivityQuery request, CancellationToken cancellationToken)
        {
            var count = request.Count <= 0 ? 10 : Math.Min(request.Count, 50);

            var query = _context.Orders.AsNoTracking().AsQueryable();
            if (request.CityId.HasValue)
            {
                query = query.Where(o => o.CityId == request.CityId.Value);
            }

            var items = await query
                .OrderByDescending(o => o.CreatedDate)
                .Take(count)
                .Select(o => new HomeRecentOrderDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    CustomerName = o.Customer.FullName,
                    SubCategoryName = o.SubCategory.Name,
                    CityName = o.City.Name,
                    Total = o.OrderTotal,
                    OrderState = (int)o.OrderState,
                    OrderStateName = o.OrderState.ToString(),
                    CreatedDate = o.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return Result.Success(new HomeRecentActivityDto
            {
                RecentOrders = items
            });
        }
    }
}
