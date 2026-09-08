using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategory.Command.UpdateSubCategoryCommand
{
    public record UpdateSubCategoryCommand : IRequest<Result<int>>
    {
        public int SubCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public bool IsOffer { get; set; } = false;
        public string? ImageUrl { get; set; }
    }

    public class UpdateSubCategoryCommandHandler : IRequestHandler<UpdateSubCategoryCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly UpdateSubCategoryCommandValidator _validator;

        public UpdateSubCategoryCommandHandler(
            DatabaseContext context, 
            IUserSession userSession,
            IImageService imageService,
            UpdateSubCategoryCommandValidator validator)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _validator = validator;
        }

        public async Task<Result<int>> Handle(UpdateSubCategoryCommand request, CancellationToken cancellationToken)
        {
            // Validate command using validator
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<int>(validationResult.Error);
            }

            var subCategory = await _context.SubCategories
                .AsTracking()
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId, cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<int>($"SubCategory with ID {request.SubCategoryId} not found");
            }

            // Verify category exists
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.IsActive, cancellationToken);

            if (category == null)
            {
                return Result.Failure<int>($"Category with ID {request.CategoryId} not found");
            }

            // Store old image URL for deletion if new image is provided
            string? oldImageUrl = subCategory.ImageUrl;

            // Save base64 image as file and get URL
            string? imageUrl = subCategory.ImageUrl; // Keep existing if no new image provided
            if (!string.IsNullOrWhiteSpace(request.ImageUrl) && _imageService.IsBase64String(request.ImageUrl))
            {
                imageUrl = _imageService.SaveBase64Image(request.ImageUrl, "subcategories");
                // Delete old image if it exists and is different
                if (!string.IsNullOrWhiteSpace(oldImageUrl) && oldImageUrl != imageUrl)
                {
                    _imageService.DeleteImage(oldImageUrl);
                }
            }

            subCategory.Update(
                request.Name,
                request.Description,
                request.CategoryId,
                request.Price,
                imageUrl,
                request.IsOffer,
                _userSession.UserName ?? "System"
            );

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(subCategory.SubCategoryId);
        }
    }
}

