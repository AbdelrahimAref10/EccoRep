using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Command.UpdateCategoryCommand
{
    public class UpdateCategoryCommandValidator
    {
        private readonly DatabaseContext _context;

        public UpdateCategoryCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            if (request.CategoryId <= 0)
            {
                return Result.Failure("Valid category ID is required");
            }

            // Validate that category exists
            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
            if (!categoryExists)
            {
                return Result.Failure($"Category with ID {request.CategoryId} not found");
            }

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

            // Check if another category with same name exists
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower().Trim() && c.CategoryId != request.CategoryId, cancellationToken);

            if (existingCategory != null)
            {
                return Result.Failure("A category with this name already exists");
            }

            return Result.Success();
        }
    }
}

