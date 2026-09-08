using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.User.Command.DeactivateUserCommand
{
    public record DeactivateUserCommand : IRequest<Result>
    {
        public int UserId { get; set; }
    }

    public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
    {
        private readonly UserManager<Domain.Models.ApplicationUser> _userManager;
        private readonly IUserSession _userSession;
        private readonly IDateTimeProvider _dateTimeProvider;

        public DeactivateUserCommandHandler(
            UserManager<Domain.Models.ApplicationUser> userManager,
            IUserSession userSession,
            IDateTimeProvider dateTimeProvider)
        {
            _userManager = userManager;
            _userSession = userSession;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            if (!user.Active)
            {
                return Result.Failure("User is already inactive");
            }

            user.Active = false;
            user.LastModifiedBy = _userSession.UserName;
            user.LastModifiedDate = _dateTimeProvider.Now;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to deactivate user: {errors}");
            }

            return Result.Success();
        }
    }
}







