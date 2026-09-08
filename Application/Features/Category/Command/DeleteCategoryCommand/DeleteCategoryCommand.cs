using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Category.Command.DeleteCategoryCommand
{
    public record DeleteCategoryCommand : IRequest<Result<bool>>
    {
        public int CategoryId { get; set; }
    }

    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public DeleteCategoryCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category == null)
            {
                return Result.Failure<bool>($"Category with ID {request.CategoryId} not found");
            }

            if (category.SubCategories.Any())
            {
                return Result.Failure<bool>(
                    "Cannot delete this category because it is linked to subcategories. Please remove or reassign the subcategories first.");
            }

            _context.Categories.Remove(category);
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to permanently delete category: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}


