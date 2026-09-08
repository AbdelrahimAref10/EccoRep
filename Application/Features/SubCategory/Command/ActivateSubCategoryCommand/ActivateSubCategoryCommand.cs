using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.SubCategory.Command.ActivateSubCategoryCommand
{
    public record ActivateSubCategoryCommand : IRequest<Result<bool>>
    {
        public int SubCategoryId { get; set; }
    }

    public class ActivateSubCategoryCommandHandler : IRequestHandler<ActivateSubCategoryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public ActivateSubCategoryCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(ActivateSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _context.SubCategories
                .AsTracking()
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId, cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<bool>($"SubCategory with ID {request.SubCategoryId} not found");
            }

            if (subCategory.IsActive)
            {
                return Result.Failure<bool>("SubCategory is already active");
            }

            subCategory.Activate(_userSession.UserName ?? "System");
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to activate subcategory: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}





