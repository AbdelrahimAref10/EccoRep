using CSharpFunctionalExtensions;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Support.Command.CreateSupportCommand
{
    public class CreateSupportCommandValidator
    {
        public Task<Result> ValidateAsync(CreateSupportCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName))
            {
                return Task.FromResult(Result.Failure("Company name is required"));
            }

            if (request.CompanyName.Trim().Length < 2)
            {
                return Task.FromResult(Result.Failure("Company name must be at least 2 characters long"));
            }

            if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Contains('@'))
            {
                return Task.FromResult(Result.Failure("Email is invalid"));
            }

            return Task.FromResult(Result.Success());
        }
    }
}
