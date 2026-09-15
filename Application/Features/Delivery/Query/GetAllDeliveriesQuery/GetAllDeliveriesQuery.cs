using Application.Features.Delivery.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Delivery.Query.GetAllDeliveriesQuery
{
    public record GetAllDeliveriesQuery : IRequest<Result<List<DeliveryDto>>>
    {
        public string? Search { get; set; }
        /// <summary>When null, returns non-deleted only. When set, filters by that value.</summary>
        public bool? IsDeleted { get; set; }
    }

    public class GetAllDeliveriesQueryHandler : IRequestHandler<GetAllDeliveriesQuery, Result<List<DeliveryDto>>>
    {
        private readonly DatabaseContext _context;

        public GetAllDeliveriesQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<DeliveryDto>>> Handle(
            GetAllDeliveriesQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Deliveries.AsNoTracking().AsQueryable();

            if (request.IsDeleted.HasValue)
                query = query.Where(d => d.IsDeleted == request.IsDeleted.Value);
            else
                query = query.Where(d => !d.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                query = query.Where(d =>
                    d.FullName.Contains(term) ||
                    d.MobileNumber.Contains(term) ||
                    (d.Email != null && d.Email.Contains(term)) ||
                    (d.User.UserName != null && d.User.UserName.Contains(term)));
            }

            var list = await query
                .OrderByDescending(d => d.CreatedDate)
                .Select(d => new DeliveryDto
                {
                    DeliveryId = d.DeliveryId,
                    UserId = d.UserId,
                    UserName = d.User.UserName,
                    CityId = d.CityId,
                    ZoneId = d.ZoneId,
                    CityName = d.City.Name,
                    FullName = d.FullName,
                    MobileNumber = d.MobileNumber,
                    Email = d.Email,
                    PersonalImage = d.PersonalImage,
                    IsActive = d.IsActive,
                    IsDeleted = d.IsDeleted,
                    CreatedDate = d.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return Result.Success(list);
        }
    }
}
