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

namespace Application.Features.AdminHome.Query.GetHomeTopPerformersQuery
{
    public record GetHomeTopPerformersQuery : IRequest<Result<HomeTopPerformersDto>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? CityId { get; set; }
        public int Top { get; set; } = 5;
    }

    public class GetHomeTopPerformersQueryHandler : IRequestHandler<GetHomeTopPerformersQuery, Result<HomeTopPerformersDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetHomeTopPerformersQueryHandler(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<HomeTopPerformersDto>> Handle(GetHomeTopPerformersQuery request, CancellationToken cancellationToken)
        {
            var top = request.Top <= 0 ? 5 : Math.Min(request.Top, 20);
            var now = _dateTimeProvider.Now;
            var (from, to) = AdminHomeDateRange.Resolve(request.From, request.To, now);

            var orders = _context.Orders.AsNoTracking()
                .Where(o => o.OrderState == OrderState.Completed && o.CreatedDate >= from && o.CreatedDate <= to);

            if (request.CityId.HasValue)
            {
                orders = orders.Where(o => o.CityId == request.CityId.Value);
            }

            var subCategories = await orders
                .GroupBy(o => new { o.SubCategoryId, o.SubCategory.Name, CategoryId = o.SubCategory.CategoryId, CategoryName = o.SubCategory.Category.Name })
                .Select(g => new
                {
                    g.Key.SubCategoryId,
                    g.Key.Name,
                    g.Key.CategoryId,
                    g.Key.CategoryName,
                    OrdersCount = g.Count(),
                    Revenue = g.SelectMany(x => x.OrderPayments).Where(p => p.State == PaymentState.Paid).Sum(p => (decimal?)p.Total) ?? 0
                })
                .OrderByDescending(x => x.Revenue)
                .Take(top)
                .ToListAsync(cancellationToken);

            var categories = await orders
                .GroupBy(o => new { o.SubCategory.CategoryId, o.SubCategory.Category.Name })
                .Select(g => new HomeTopItemDto
                {
                    Id = g.Key.CategoryId,
                    Name = g.Key.Name,
                    OrdersCount = g.Count(),
                    Revenue = g.SelectMany(x => x.OrderPayments).Where(p => p.State == PaymentState.Paid).Sum(p => (decimal?)p.Total) ?? 0
                })
                .OrderByDescending(x => x.Revenue)
                .Take(top)
                .ToListAsync(cancellationToken);

            return Result.Success(new HomeTopPerformersDto
            {
                Categories = categories,
                SubCategories = subCategories.Select(x => new HomeTopItemDto
                {
                    Id = x.SubCategoryId,
                    Name = x.Name,
                    OrdersCount = x.OrdersCount,
                    Revenue = x.Revenue
                }).ToList(),
                From = from,
                To = to
            });
        }
    }
}
