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

namespace Application.Features.City.Command.AddCityCommand
{
    public record AddCityCommand : IRequest<Result<int>>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? DeliveryFees { get; set; } // Amount value (per vehicle)
        public decimal? UrgentDelivery { get; set; } // Amount value
        public decimal? ServiceFees { get; set; } // Amount value
        public decimal? CancellationFees { get; set; } // Percentage value (e.g., 5.0 means 5%)
        public List<TieredDiscountDto>? TieredDiscounts { get; set; }
    }

    public class AddCityCommandHandler : IRequestHandler<AddCityCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly AddCityCommandValidator _validator;

        public AddCityCommandHandler(
            DatabaseContext context, 
            IUserSession userSession,
            AddCityCommandValidator validator)
        {
            _context = context;
            _userSession = userSession;
            _validator = validator;
        }

        public async Task<Result<int>> Handle(AddCityCommand request, CancellationToken cancellationToken)
        {
            // Validate command using validator
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<int>(validationResult.Error);
            }

            var city = Domain.Models.City.Create(
                request.Name,
                request.Description,
                request.DeliveryFees,
                request.UrgentDelivery,
                request.ServiceFees,
                request.CancellationFees,
                _userSession.UserName ?? "System"
            );

            _context.Cities.Add(city);
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<int>($"Failed to add city: {saveResult.ErrorMessage}");
            }

            // Add tiered discounts if provided
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

                saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
                if (!saveResult.IsSuccess)
                {
                    return Result.Failure<int>($"Failed to add tiered discounts: {saveResult.ErrorMessage}");
                }
            }

            return Result.Success(city.CityId);
        }
    }
}

