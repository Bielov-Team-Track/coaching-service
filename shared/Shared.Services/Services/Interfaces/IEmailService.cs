namespace Shared.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string templateName, Dictionary<string, string> parameters);
    }
}
