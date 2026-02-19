namespace Shared.Models.Jwt
{
    public class JwtPayloadDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public required string AccessToken { get; init; }
    }
}
