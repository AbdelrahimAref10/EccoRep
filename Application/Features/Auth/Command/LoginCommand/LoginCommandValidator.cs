using CSharpFunctionalExtensions;

namespace Application.Features.Auth.Command.LoginCommand
{
    public class LoginCommandValidator
    {
        public Task<Result> ValidateAsync(LoginCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return Task.FromResult(Result.Failure("User name is required"));

            if (string.IsNullOrWhiteSpace(request.Password))
                return Task.FromResult(Result.Failure("Password is required"));

            return Task.FromResult(Result.Success());
        }
    }
}
