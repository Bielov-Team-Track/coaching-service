namespace Shared.Contracts.Models;

public class CreateUserProfileRequest
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class CreateUserProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ProfileId { get; set; }
}

public class UserProfileInfo
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}