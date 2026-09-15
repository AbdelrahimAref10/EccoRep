using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.City.Command.AddCityCommand
{
    public class AddCityCommandValidator
    {
        private readonly DatabaseContext _context;

        public AddCityCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(AddCityCommand request, CancellationToken cancellationToken)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure("City name is required");
            }

            if (request.Name.Trim().Length < 2)
            {
                return Result.Failure("City name must be at least 2 characters long");
            }

            // Validate fees are non-negative when provided
            if (request.UrgentDelivery.HasValue && request.UrgentDelivery.Value < 0)
                return Result.Failure("Urgent delivery fees cannot be negative");
            if (request.ServiceFees.HasValue && request.ServiceFees.Value < 0)
                return Result.Failure("Service fees cannot be negative");
            if (request.CancellationFees.HasValue && (request.CancellationFees.Value < 0 || request.CancellationFees.Value > 100))
                return Result.Failure("Cancellation fees must be between 0 and 100 (percentage)");

            // Check if city with same name already exists
            var existingCity = await _context.Cities
                .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower().Trim(), cancellationToken);

            if (existingCity != null)
            {
                return Result.Failure("A city with this name already exists");
            }

            // Validate tiered discounts if provided
            if (request.TieredDiscounts != null && request.TieredDiscounts.Any())
            {
                foreach (var tieredDiscount in request.TieredDiscounts)
                {
                    if (tieredDiscount.From < 0)
                        return Result.Failure("Tiered discount 'From' days cannot be negative");

                    if (tieredDiscount.To < tieredDiscount.From)
                        return Result.Failure("Tiered discount 'To' days must be greater than or equal to 'From' days");

                    if (tieredDiscount.Discount < 0 || tieredDiscount.Discount > 100)
                        return Result.Failure("Tiered discount must be between 0 and 100");
                }
            }

            if (request.ZoneGroupId.HasValue && request.ZoneGroupId.Value > 0)
            {
                var groupExists = await _context.ZoneGroups.AnyAsync(
                    g => g.ZoneGroupId == request.ZoneGroupId.Value && g.IsActive, cancellationToken);
                if (!groupExists)
                    return Result.Failure("Zone group not found or inactive");
            }

            return Result.Success();
        }
    }
}

