namespace Shared.Models;

public class UserProfile : BaseEntity
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageThumbHash { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsActive { get; set; }
}
