using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Command.DeactivateCategoryCommand
{
    public record DeactivateCategoryCommand : IRequest<Result<bool>>
    {
        public int CategoryId { get; set; }
    }

    public class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public DeactivateCategoryCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .AsTracking()
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category == null)
            {
                return Result.Failure<bool>($"Category with ID {request.CategoryId} not found");
            }

            if (!category.IsActive)
            {
                return Result.Failure<bool>("Category is already inactive");
            }

            category.Deactivate(_userSession.UserName ?? "System");
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to deactivate category: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}





