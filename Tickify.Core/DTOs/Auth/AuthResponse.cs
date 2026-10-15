using Tickify.Core.Enums;

namespace Tickify.Core.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
