using Application.Features.City.DTOs;
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

namespace Application.Features.City.Command.UpdateCityCommand
{
    public record UpdateCityCommand : IRequest<Result<int>>
    {
        public int CityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? DeliveryFees { get; set; } // Amount value (per vehicle)
        public decimal? UrgentDelivery { get; set; } // Amount value
        public decimal? ServiceFees { get; set; } // Amount value
        public decimal? CancellationFees { get; set; } // Percentage value (e.g., 5.0 means 5%)
        public List<TieredDiscountDto>? TieredDiscounts { get; set; }
    }

    public class UpdateCityCommandHandler : IRequestHandler<UpdateCityCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly UpdateCityCommandValidator _validator;

        public UpdateCityCommandHandler(
            DatabaseContext context, 
            IUserSession userSession,
            UpdateCityCommandValidator validator)
        {
            _context = context;
            _userSession = userSession;
            _validator = validator;
        }

        public async Task<Result<int>> Handle(UpdateCityCommand request, CancellationToken cancellationToken)
        {
            // Validate command using validator
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<int>(validationResult.Error);
            }

            var city = await _context.Cities
                .Include(c => c.TieredDiscounts)
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CityId == request.CityId, cancellationToken);

            if (city == null)
            {
                return Result.Failure<int>($"City with ID {request.CityId} not found");
            }

            city.Update(
                request.Name,
                request.Description,
                _userSession.UserName ?? "System"
            );

            city.UpdateFees(
                request.DeliveryFees ?? 0,
                request.UrgentDelivery ?? 0,
                request.ServiceFees ?? 0,
                request.CancellationFees ?? 0,
                _userSession.UserName ?? "System"
            );

            // Handle tiered discounts: remove existing and add new ones
            // Use the tiered discounts already loaded from Include to avoid tracking conflicts
            var existingTieredDiscounts = city.TieredDiscounts.ToList();

            if (existingTieredDiscounts.Any())
            {
                _context.TieredDiscounts.RemoveRange(existingTieredDiscounts);
            }

            // Add new tiered discounts if provided
            if (request.TieredDiscounts != null && request.TieredDiscounts.Any())
            {
                foreach (var tieredDiscountDto in request.TieredDiscounts)
                {
                    var tieredDiscount = Domain.Models.TieredDiscount.Create(
                        city.CityId,
                        tieredDiscountDto.From,
                        tieredDiscountDto.To,
                        tieredDiscountDto.Discount,
                        _userSession.UserName ?? "System"
                    );
                    _context.TieredDiscounts.Add(tieredDiscount);
                }
            }

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<int>($"Failed to update city: {saveResult.ErrorMessage}");
            }

            return Result.Success(city.CityId);
        }
    }
}

