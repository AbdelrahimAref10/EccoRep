using Application.Features.SubCategory.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategory.Query.GetSubCategoriesLookupQuery
{
    public record GetSubCategoriesLookupQuery : IRequest<Result<List<SubCategoryLookupDto>>>
    {
        public int? CategoryId { get; set; }
    }

    public class GetSubCategoriesLookupQueryHandler : IRequestHandler<GetSubCategoriesLookupQuery, Result<List<SubCategoryLookupDto>>>
    {
        private readonly DatabaseContext _context;

        public GetSubCategoriesLookupQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<SubCategoryLookupDto>>> Handle(GetSubCategoriesLookupQuery request, CancellationToken cancellationToken)
        {
            var query = _context.SubCategories
                .Include(sc => sc.Category)
                .Where(sc => sc.IsActive)
                .AsQueryable();

            if (request.CategoryId.HasValue)
            {
                query = query.Where(sc => sc.CategoryId == request.CategoryId.Value);
            }

            var subCategories = await query
                .OrderBy(sc => sc.Name)
                .Select(sc => new SubCategoryLookupDto
                {
                    SubCategoryId = sc.SubCategoryId,
                    Name = sc.Name,
                    CategoryId = sc.CategoryId,
                    CategoryName = sc.Category.Name,
                    Price = sc.Price
                })
                .ToListAsync(cancellationToken);

            return Result.Success(subCategories);
        }
    }
}

