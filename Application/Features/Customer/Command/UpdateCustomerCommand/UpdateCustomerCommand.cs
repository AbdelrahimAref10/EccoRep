using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.UpdateCustomerCommand
{
    public record UpdateCustomerCommand : IRequest<Result<bool>>
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string? Email { get; set; }
        public string? PersonalImage { get; set; }
        public string? CommercialRegisterImage { get; set; }
    }

    public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly UpdateCustomerCommandValidator _validator;

        public UpdateCustomerCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService,
            UpdateCustomerCommandValidator validator)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _validator = validator;
        }

        public async Task<Result<bool>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            // Validate command using validator
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<bool>(validationResult.Error);
            }

            var customer = await _context.Customers
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<bool>("Customer not found");
            }

            // Save base64 images as files and get URLs
            // Store old image URLs for deletion if new images are provided
            string? oldPersonalImage = customer.PersonalImage;
            string? oldCommercialRegisterImage = customer.CommercialRegisterImage;

            string? personalImageUrl = customer.PersonalImage; // Keep existing if no new image provided
            if (!string.IsNullOrWhiteSpace(request.PersonalImage) && _imageService.IsBase64String(request.PersonalImage))
            {
                personalImageUrl = _imageService.SaveBase64Image(request.PersonalImage, "customers");
                // Delete old image if it exists and is different
                if (!string.IsNullOrWhiteSpace(oldPersonalImage) && oldPersonalImage != personalImageUrl)
                {
                    _imageService.DeleteImage(oldPersonalImage);
                }
            }

            string? commercialRegisterImageUrl = customer.CommercialRegisterImage; // Keep existing if no new image provided
            if (!string.IsNullOrWhiteSpace(request.CommercialRegisterImage) && _imageService.IsBase64String(request.CommercialRegisterImage))
            {
                commercialRegisterImageUrl = _imageService.SaveBase64Image(request.CommercialRegisterImage, "customers");
                // Delete old image if it exists and is different
                if (!string.IsNullOrWhiteSpace(oldCommercialRegisterImage) && oldCommercialRegisterImage != commercialRegisterImageUrl)
                {
                    _imageService.DeleteImage(oldCommercialRegisterImage);
                }
            }

            customer.UpdateProfile(
                request.FullName,
                request.Gender,
                request.CityId,
                request.Email,
                personalImageUrl,
                commercialRegisterImageUrl,
                _userSession.UserId.ToString()
            );

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to update customer: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}

