using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Command.CreateCategoryCommand
{
    public record CreateCategoryCommand : IRequest<Result<int>>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<int>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly CreateCategoryCommandValidator _validator;

        public CreateCategoryCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService,
            CreateCategoryCommandValidator validator)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _validator = validator;
        }

        public async Task<Result<int>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            // Validate command using validator
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<int>(validationResult.Error);
            }

            // Verify city exists
            var city = await _context.Cities
                .FirstOrDefaultAsync(c => c.CityId == request.CityId && c.IsActive, cancellationToken);

            if (city == null)
            {
                return Result.Failure<int>("City not found or is not active");
            }

            // Save base64 image as file and get URL
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                imageUrl = _imageService.SaveBase64Image(request.ImageUrl, "categories");
            }

            var category = Domain.Models.Category.Create(
                request.Name,
                request.Description,
                request.CityId,
                imageUrl,
                _userSession.UserName ?? "System"
            );

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(category.CategoryId);
        }
    }
}

