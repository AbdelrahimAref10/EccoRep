namespace Domain.Common
{
    public interface IUserSession
    {
        int UserId { get; }
        string? UserName { get; }
        string? Email { get; }
        List<string> Roles { get; }
        bool IsAuthenticated { get; }
    }
}

