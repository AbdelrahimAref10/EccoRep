using Application.Common;
using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetCustomerOrdersQuery
{
    public record GetCustomerOrdersQuery : IRequest<Result<PagedResult<OrderDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, Result<PagedResult<OrderDto>>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetCustomerOrdersQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<PagedResult<OrderDto>>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
        {
            if (_userSession.UserId <= 0)
            {
                return Result.Failure<PagedResult<OrderDto>>("Customer not found or not authenticated");
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == _userSession.UserId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<PagedResult<OrderDto>>("Customer not found");
            }

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.SubCategory)
                .Include(o => o.City)
                .Where(o => o.CustomerId == customer.CustomerId)
                .OrderByDescending(o => o.CreatedDate)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var orders = await query
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
                    DestinationZoneId = o.DestinationZoneId,
                    ReservationDateFrom = o.ReservationDateFrom,
                    ReservationDateTo = o.ReservationDateTo,
                    VehiclesCount = o.VehiclesCount,
                    OrderSubTotal = o.OrderSubTotal,
                    OrderTotal = o.OrderTotal,
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

