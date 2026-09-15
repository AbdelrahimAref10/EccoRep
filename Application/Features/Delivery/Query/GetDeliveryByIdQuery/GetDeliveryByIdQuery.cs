using Application.Features.Delivery.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Delivery.Query.GetDeliveryByIdQuery
{
    public record GetDeliveryByIdQuery : IRequest<Result<DeliveryDto>>
    {
        public int DeliveryId { get; set; }
    }

    public class GetDeliveryByIdQueryHandler : IRequestHandler<GetDeliveryByIdQuery, Result<DeliveryDto>>
    {
        private readonly DatabaseContext _context;

        public GetDeliveryByIdQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<DeliveryDto>> Handle(GetDeliveryByIdQuery request, CancellationToken cancellationToken)
        {
            if (request.DeliveryId <= 0)
                return Result.Failure<DeliveryDto>("DeliveryId is required");

            var delivery = await _context.Deliveries
                .AsNoTracking()
                .Where(d => d.DeliveryId == request.DeliveryId && !d.IsDeleted)
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
                .FirstOrDefaultAsync(cancellationToken);

            if (delivery == null)
                return Result.Failure<DeliveryDto>("Delivery not found");

            return Result.Success(delivery);
        }
    }
}
