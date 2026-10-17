using Tickify.Core.Enums;

namespace Tickify.Core.DTOs.Users;

/// <summary>Public profile of a user. Deliberately without e-mail and without anything credential-related.</summary>
public class UserResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
