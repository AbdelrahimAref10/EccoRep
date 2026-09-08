using Application.Features.Support.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Support.Command.CreateSupportCommand
{
    public record CreateSupportCommand : IRequest<Result<SupportDto>>
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public string? YouTubeUrl { get; set; }
        public string? WorkingHours { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? AdditionalInfo { get; set; }
    }

    public class CreateSupportCommandHandler : IRequestHandler<CreateSupportCommand, Result<SupportDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly CreateSupportCommandValidator _validator;

        public CreateSupportCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            CreateSupportCommandValidator validator)
        {
            _context = context;
            _userSession = userSession;
            _validator = validator;
        }

        public async Task<Result<SupportDto>> Handle(CreateSupportCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<SupportDto>(validationResult.Error);
            }

            var exists = await _context.Supports.AnyAsync(cancellationToken);
            if (exists)
            {
                return Result.Failure<SupportDto>("Support contact info already exists. Use update instead.");
            }

            var support = Domain.Models.Support.Create(
                request.CompanyName,
                request.Address,
                request.PhoneNumber,
                request.WhatsAppNumber,
                request.Email,
                request.WebsiteUrl,
                request.FacebookUrl,
                request.InstagramUrl,
                request.TwitterUrl,
                request.LinkedInUrl,
                request.TikTokUrl,
                request.YouTubeUrl,
                request.WorkingHours,
                request.Latitude,
                request.Longitude,
                request.AdditionalInfo,
                _userSession.UserName ?? "System"
            );

            _context.Supports.Add(support);
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<SupportDto>($"Failed to create support info: {saveResult.ErrorMessage}");
            }

            return Result.Success(MapToDto(support));
        }

        private static SupportDto MapToDto(Domain.Models.Support support) => new()
        {
            SupportId = support.SupportId,
            CompanyName = support.CompanyName,
            Address = support.Address,
            PhoneNumber = support.PhoneNumber,
            WhatsAppNumber = support.WhatsAppNumber,
            Email = support.Email,
            WebsiteUrl = support.WebsiteUrl,
            FacebookUrl = support.FacebookUrl,
            InstagramUrl = support.InstagramUrl,
            TwitterUrl = support.TwitterUrl,
            LinkedInUrl = support.LinkedInUrl,
            TikTokUrl = support.TikTokUrl,
            YouTubeUrl = support.YouTubeUrl,
            WorkingHours = support.WorkingHours,
            Latitude = support.Latitude,
            Longitude = support.Longitude,
            AdditionalInfo = support.AdditionalInfo
        };
    }
}
