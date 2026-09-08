using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategory.Command.CreateSubCategoryCommand
{
    public record CreateSubCategoryCommand : IRequest<Result<int>>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public bool IsOffer { get; set; } = false;
        public string? ImageUrl { get; set; }
    }

    public class CreateSubCategoryCommandHandler : IRequestHandler<CreateSubCategoryCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly CreateSubCategoryCommandValidator _validator;

        public CreateSubCategoryCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService,
            CreateSubCategoryCommandValidator validator)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _validator = validator;
        }

        public async Task<Result<int>> Handle(CreateSubCategoryCommand request, CancellationToken cancellationToken)
        {
            // Validate command using validator
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<int>(validationResult.Error);
            }

            // Verify category exists
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.IsActive, cancellationToken);

            if (category == null)
            {
                return Result.Failure<int>($"Category with ID {request.CategoryId} not found");
            }

            // Save base64 image as file and get URL
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                imageUrl = _imageService.SaveBase64Image(request.ImageUrl, "subcategories");
            }

            var subCategory = Domain.Models.SubCategory.Create(
                request.Name,
                request.Description,
                request.CategoryId,
                request.Price,
                imageUrl,
                request.IsOffer,
                _userSession.UserName ?? "System"
            );

            _context.SubCategories.Add(subCategory);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(subCategory.SubCategoryId);
        }
    }
}

