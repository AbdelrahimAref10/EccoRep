using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategory.Command.DeleteSubCategoryCommand
{
    public record DeleteSubCategoryCommand : IRequest<Result<bool>>
    {
        public int SubCategoryId { get; set; }
    }

    public class DeleteSubCategoryCommandHandler : IRequestHandler<DeleteSubCategoryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public DeleteSubCategoryCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(DeleteSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _context.SubCategories
                .Include(sc => sc.Vehicles)
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId, cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<bool>($"SubCategory with ID {request.SubCategoryId} not found");
            }

            if (subCategory.Vehicles.Any())
            {
                return Result.Failure<bool>(
                    "Cannot delete this subcategory because it is linked to vehicles. Please remove or reassign the vehicles first.");
            }

            _context.SubCategories.Remove(subCategory);
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to permanently delete subcategory: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}

