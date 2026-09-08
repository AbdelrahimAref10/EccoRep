using Application.Common;
using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetAllOrdersQuery
{
    public record GetAllOrdersQuery : IRequest<Result<PagedResult<OrderDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public OrderState? State { get; set; }
        public string? OrderCode { get; set; }
    }

    public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, Result<PagedResult<OrderDto>>>
    {
        private readonly DatabaseContext _context;

        public GetAllOrdersQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<PagedResult<OrderDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.SubCategory)
                .Include(o => o.City)
                .AsQueryable();

            // Apply filters
            if (request.State.HasValue)
            {
                query = query.Where(o => o.OrderState == request.State.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.OrderCode))
            {
                query = query.Where(o => o.OrderCode.Contains(request.OrderCode));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var orders = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new OrderDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer.FullName,
                    SubCategoryId = o.SubCategoryId,
                    SubCategoryName = o.SubCategory.Name,
                    CityId = o.CityId,
                    CityName = o.City.Name,
                    ReservationDateFrom = o.ReservationDateFrom,
                    ReservationDateTo = o.ReservationDateTo,
                    VehiclesCount = o.VehiclesCount,
                    OrderSubTotal = o.OrderSubTotal,
                    OrderTotal = o.OrderTotal,
                    PreviousDebt = o.PreviousDebt,
                    MoneyRefunded = o.MoneyRefunded,
                    Notes = o.Notes,
                    HotelName = o.HotelName,
                    HotelAddress = o.HotelAddress,
                    HotelPhone = o.HotelPhone,
                    IsUrgent = o.IsUrgent,
                    PaymentMethod = (PaymentMethod)o.PaymentMethodId,
                    OrderState = o.OrderState,
                    CreatedDate = o.CreatedDate
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResult<OrderDto>
            {
                Items = orders,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return Result.Success(result);
        }
    }
}

