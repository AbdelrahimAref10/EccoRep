using Domain.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Services
{
    public class UserSession : IUserSession
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserSession(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(userIdClaim, out var userId) ? userId : 0;
            }
        }

        public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

        public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

        public List<string> Roles
        {
            get
            {
                var roles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList();
                return roles ?? new List<string>();
            }
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}

