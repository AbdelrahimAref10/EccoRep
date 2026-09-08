using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategory.Command.DeactivateSubCategoryCommand
{
    public record DeactivateSubCategoryCommand : IRequest<Result<bool>>
    {
        public int SubCategoryId { get; set; }
    }

    public class DeactivateSubCategoryCommandHandler : IRequestHandler<DeactivateSubCategoryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public DeactivateSubCategoryCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(DeactivateSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _context.SubCategories
                .AsTracking()
                .Include(sc => sc.Vehicles)
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId, cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<bool>($"SubCategory with ID {request.SubCategoryId} not found");
            }

            if (!subCategory.IsActive)
            {
                return Result.Failure<bool>("SubCategory is already inactive");
            }

            subCategory.Deactivate(_userSession.UserName ?? "System");
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to deactivate subcategory: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}





