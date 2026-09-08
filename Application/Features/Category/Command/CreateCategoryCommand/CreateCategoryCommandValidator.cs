using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Command.CreateCategoryCommand
{
    public class CreateCategoryCommandValidator
    {
        private readonly DatabaseContext _context;

        public CreateCategoryCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure("Category name is required");
            }

            if (request.Name.Trim().Length < 2)
            {
                return Result.Failure("Category name must be at least 2 characters long");
            }

            if (request.CityId <= 0)
            {
                return Result.Failure("City ID must be greater than zero");
            }

            // Check if city exists and is active
            var cityExists = await _context.Cities
                .AnyAsync(c => c.CityId == request.CityId && c.IsActive, cancellationToken);

            if (!cityExists)
            {
                return Result.Failure("City not found or is not active");
            }

            // Check if category with same name already exists
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower().Trim(), cancellationToken);

            if (existingCategory != null)
            {
                return Result.Failure("A category with this name already exists");
            }

            return Result.Success();
        }
    }
}

