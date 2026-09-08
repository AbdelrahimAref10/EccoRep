using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Command.UpdateCategoryCommand
{
    public record UpdateCategoryCommand : IRequest<Result<int>>
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly UpdateCategoryCommandValidator _validator;

        public UpdateCategoryCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService,
            UpdateCategoryCommandValidator validator)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _validator = validator;
        }

        public async Task<Result<int>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            // Validate command using validator
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<int>(validationResult.Error);
            }

            var category = await _context.Categories
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category == null)
            {
                return Result.Failure<int>($"Category with ID {request.CategoryId} not found");
            }

            // Verify city exists
            var city = await _context.Cities
                .FirstOrDefaultAsync(c => c.CityId == request.CityId && c.IsActive, cancellationToken);

            if (city == null)
            {
                return Result.Failure<int>("City not found or is not active");
            }

            // Store old image URL for deletion if new image is provided
            string? oldImageUrl = category.ImageUrl;

            // Save base64 image as file and get URL
            string? imageUrl = category.ImageUrl; // Keep existing if no new image provided
            if (!string.IsNullOrWhiteSpace(request.ImageUrl) && _imageService.IsBase64String(request.ImageUrl))
            {
                imageUrl = _imageService.SaveBase64Image(request.ImageUrl, "categories");
                // Delete old image if it exists and is different
                if (!string.IsNullOrWhiteSpace(oldImageUrl) && oldImageUrl != imageUrl)
                {
                    _imageService.DeleteImage(oldImageUrl);
                }
            }

            category.Update(
                request.Name,
                request.Description,
                request.CityId,
                imageUrl,
                _userSession.UserName ?? "System"
            );

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(category.CategoryId);
        }
    }
}


