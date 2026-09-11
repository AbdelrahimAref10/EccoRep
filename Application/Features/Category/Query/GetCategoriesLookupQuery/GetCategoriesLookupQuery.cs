using Application.Features.Category.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Query.GetCategoriesLookupQuery
{
    public record GetCategoriesLookupQuery : IRequest<Result<List<CategoryLookupDto>>>
    {
        /// <summary>When set, returns only active categories for this city.</summary>
        public int? CityId { get; set; }
    }

    public class GetCategoriesLookupQueryHandler : IRequestHandler<GetCategoriesLookupQuery, Result<List<CategoryLookupDto>>>
    {
        private readonly DatabaseContext _context;

        public GetCategoriesLookupQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<CategoryLookupDto>>> Handle(GetCategoriesLookupQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Categories.AsNoTracking().Where(c => c.IsActive);

            if (request.CityId is > 0)
                query = query.Where(c => c.CityId == request.CityId.Value);

            var categories = await query
                .OrderBy(c => c.Name)
                .Select(c => new CategoryLookupDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name
                })
                .ToListAsync(cancellationToken);

            return Result.Success(categories);
        }
    }
}
