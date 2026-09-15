using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Zone.Query.GetZoneGroupsLookupQuery
{
    public record ZoneGroupLookupDto
    {
        public int ZoneGroupId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public record GetZoneGroupsLookupQuery : IRequest<Result<List<ZoneGroupLookupDto>>>
    {
    }

    public class GetZoneGroupsLookupQueryHandler : IRequestHandler<GetZoneGroupsLookupQuery, Result<List<ZoneGroupLookupDto>>>
    {
        private readonly DatabaseContext _context;

        public GetZoneGroupsLookupQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<ZoneGroupLookupDto>>> Handle(GetZoneGroupsLookupQuery request, CancellationToken cancellationToken)
        {
            var groups = await _context.ZoneGroups.AsNoTracking()
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .Select(g => new ZoneGroupLookupDto { ZoneGroupId = g.ZoneGroupId, Name = g.Name })
                .ToListAsync(cancellationToken);

            return Result.Success(groups);
        }
    }
}
