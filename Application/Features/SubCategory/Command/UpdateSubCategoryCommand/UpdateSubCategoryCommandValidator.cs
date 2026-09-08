using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategory.Command.UpdateSubCategoryCommand
{
    public class UpdateSubCategoryCommandValidator
    {
        private readonly DatabaseContext _context;

        public UpdateSubCategoryCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(UpdateSubCategoryCommand request, CancellationToken cancellationToken)
        {
            if (request.SubCategoryId <= 0)
            {
                return Result.Failure("Valid subcategory ID is required");
            }

            // Validate that subcategory exists
            var subCategoryExists = await _context.SubCategories
                .AnyAsync(sc => sc.SubCategoryId == request.SubCategoryId, cancellationToken);

            if (!subCategoryExists)
            {
                return Result.Failure($"SubCategory with ID {request.SubCategoryId} not found");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure("SubCategory name is required");
            }

            if (request.Name.Trim().Length < 2)
            {
                return Result.Failure("SubCategory name must be at least 2 characters long");
            }

            if (request.Name.Trim().Length > 256)
            {
                return Result.Failure("SubCategory name must not exceed 256 characters");
            }

            if (request.Description != null && request.Description.Length > 1000)
            {
                return Result.Failure("Description must not exceed 1000 characters");
            }

            if (request.CategoryId <= 0)
            {
                return Result.Failure("Valid category ID is required");
            }

            // Verify category exists
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == request.CategoryId && c.IsActive, cancellationToken);

            if (!categoryExists)
            {
                return Result.Failure($"Category with ID {request.CategoryId} not found");
            }

            if (request.Price < 0)
            {
                return Result.Failure("Price must be greater than or equal to zero");
            }

            // Check if another subcategory with same name exists in this category
            var existingSubCategory = await _context.SubCategories
                .FirstOrDefaultAsync(sc => sc.Name.ToLower() == request.Name.ToLower().Trim() && 
                                          sc.CategoryId == request.CategoryId &&
                                          sc.SubCategoryId != request.SubCategoryId, cancellationToken);

            if (existingSubCategory != null)
            {
                return Result.Failure("A subcategory with this name already exists in this category");
            }

            return Result.Success();
        }
    }
}

