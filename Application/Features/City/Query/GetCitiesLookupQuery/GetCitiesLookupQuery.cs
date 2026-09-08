using Application.Features.City.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.City.Query.GetCitiesLookupQuery
{
    public record GetCitiesLookupQuery : IRequest<Result<List<CityLookupDto>>>;

    public class GetCitiesLookupQueryHandler : IRequestHandler<GetCitiesLookupQuery, Result<List<CityLookupDto>>>
    {
        private readonly DatabaseContext _context;

        public GetCitiesLookupQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<CityLookupDto>>> Handle(GetCitiesLookupQuery request, CancellationToken cancellationToken)
        {
            var cities = await _context.Cities
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new CityLookupDto
                {
                    CityId = c.CityId,
                    Name = c.Name
                })
                .ToListAsync(cancellationToken);

            return Result.Success(cities);
        }
    }
}

