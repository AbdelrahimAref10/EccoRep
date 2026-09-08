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
    public record GetCategoriesLookupQuery : IRequest<Result<List<CategoryLookupDto>>>;

    public class GetCategoriesLookupQueryHandler : IRequestHandler<GetCategoriesLookupQuery, Result<List<CategoryLookupDto>>>
    {
        private readonly DatabaseContext _context;

        public GetCategoriesLookupQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<CategoryLookupDto>>> Handle(GetCategoriesLookupQuery request, CancellationToken cancellationToken)
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
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


