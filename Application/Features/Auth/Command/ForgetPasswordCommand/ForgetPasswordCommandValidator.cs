using CSharpFunctionalExtensions;

namespace Application.Features.Auth.Command.ForgetPasswordCommand
{
    public class ForgetPasswordCommandValidator
    {
        public Task<Result> ValidateAsync(ForgetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.MobileNumber) && string.IsNullOrWhiteSpace(request.Email))
                return Task.FromResult(Result.Failure("Mobile number or email is required"));

            return Task.FromResult(Result.Success());
        }
    }
}
