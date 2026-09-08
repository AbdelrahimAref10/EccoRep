using Application.Features.Support.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Support.Query.GetSupportQuery
{
    public record GetSupportQuery : IRequest<Result<SupportDto>>;

    public class GetSupportQueryHandler : IRequestHandler<GetSupportQuery, Result<SupportDto>>
    {
        private readonly DatabaseContext _context;

        public GetSupportQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<SupportDto>> Handle(GetSupportQuery request, CancellationToken cancellationToken)
        {
            var support = await _context.Supports
                .OrderBy(s => s.SupportId)
                .FirstOrDefaultAsync(cancellationToken);

            if (support == null)
            {
                return Result.Failure<SupportDto>("Support contact info not found");
            }

            return Result.Success(new SupportDto
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
            });
        }
    }
}
