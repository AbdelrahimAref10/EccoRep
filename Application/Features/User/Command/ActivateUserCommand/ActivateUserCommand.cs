using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.User.Command.ActivateUserCommand
{
    public record ActivateUserCommand : IRequest<Result>
    {
        public int UserId { get; set; }
    }

    public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, Result>
    {
        private readonly UserManager<Domain.Models.ApplicationUser> _userManager;
        private readonly IUserSession _userSession;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ActivateUserCommandHandler(
            UserManager<Domain.Models.ApplicationUser> userManager,
            IUserSession userSession,
            IDateTimeProvider dateTimeProvider)
        {
            _userManager = userManager;
            _userSession = userSession;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            if (user.Active)
            {
                return Result.Failure("User is already active");
            }

            user.Active = true;
            user.LastModifiedBy = _userSession.UserName;
            user.LastModifiedDate = _dateTimeProvider.Now;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to activate user: {errors}");
            }

            return Result.Success();
        }
    }
}

