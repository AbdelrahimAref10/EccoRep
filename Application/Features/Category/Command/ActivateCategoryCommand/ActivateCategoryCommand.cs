using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Command.ActivateCategoryCommand
{
    public record ActivateCategoryCommand : IRequest<Result<bool>>
    {
        public int CategoryId { get; set; }
    }

    public class ActivateCategoryCommandHandler : IRequestHandler<ActivateCategoryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public ActivateCategoryCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(ActivateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category == null)
            {
                return Result.Failure<bool>($"Category with ID {request.CategoryId} not found");
            }

            if (category.IsActive)
            {
                return Result.Failure<bool>("Category is already active");
            }

            category.Activate(_userSession.UserName ?? "System");
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to activate category: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}





