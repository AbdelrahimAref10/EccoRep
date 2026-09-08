using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.User.Command.DeleteUserCommand
{
    public record DeleteUserCommand(int UserId) : IRequest<Result>;

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
    {
        private readonly UserManager<Domain.Models.ApplicationUser> _userManager;

        public DeleteUserCommandHandler(UserManager<Domain.Models.ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to delete user: {errors}");
            }

            return Result.Success();
        }
    }
}

