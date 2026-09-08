using Application.Features.Auth.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Command.RefreshTokenCommand
{
    public record RefreshTokenCommand : IRequest<Result<RefreshTokenResponse>>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IJwtSettings _jwtSettings;
        private readonly IDateTimeProvider _dateTimeProvider;

        public RefreshTokenCommandHandler(
            DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IJwtSettings jwtSettings,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _jwtSettings = jwtSettings;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Result.Failure<RefreshTokenResponse>("Refresh token is required");

            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (refreshToken == null)
                return Result.Failure<RefreshTokenResponse>("Invalid refresh token");

            if (!refreshToken.IsActive(_dateTimeProvider))
                return Result.Failure<RefreshTokenResponse>("Refresh token has been revoked or expired");

            var user = refreshToken.User;
            if (user == null)
                return Result.Failure<RefreshTokenResponse>("User not found");

            if (!user.Active)
            {
                refreshToken.Revoke(_dateTimeProvider);
                await _context.SaveChangesAsync(cancellationToken);
                return Result.Failure<RefreshTokenResponse>("User account is not active");
            }

            var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
            refreshToken.Revoke(_dateTimeProvider, newRefreshTokenValue);

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtTokenService.GenerateToken(user, roles);

            await _context.RefreshTokens.AddAsync(RefreshToken.Create(
                user.Id,
                newRefreshTokenValue,
                _dateTimeProvider.Now.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                user.UserName ?? "System",
                _dateTimeProvider), cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(new RefreshTokenResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshTokenValue
            });
        }
    }
}
