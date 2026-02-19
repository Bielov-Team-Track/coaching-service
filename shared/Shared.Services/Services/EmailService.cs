using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Options;
using Shared.Services.Interfaces;

namespace Shared.Services
{
    public class EmailService : IEmailService
    {
        private readonly IAmazonSimpleEmailServiceV2 _sesClient;
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailOptions;
        private readonly string _templatesPath;

        public EmailService(
            IAmazonSimpleEmailServiceV2 sesClient,
            ILogger<EmailService> logger,
            IOptions<EmailSettings> emailOptions)
        {
            _sesClient = sesClient;
            _logger = logger;
            _templatesPath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates");
            _emailOptions = emailOptions.Value;
        }

        public async Task SendEmailAsync(string email, string templateName, Dictionary<string, string> parameters)
        {
            try
            {
                var template = await LoadTemplateAsync(templateName);
                var processedTemplate = ProcessTemplate(template, parameters);

                var fromEmail = _emailOptions.FromEmail;
                var request = new SendEmailRequest
                {
                    FromEmailAddress = fromEmail,
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { email }
                    },
                    Content = new EmailContent
                    {
                        Simple = new Message
                        {
                            Subject = new Content
                            {
                                Data = processedTemplate.Subject,
                                Charset = "UTF-8"
                            },
                            Body = new Body
                            {
                                Html = new Content
                                {
                                    Data = processedTemplate.Body,
                                    Charset = "UTF-8"
                                }
                            }
                        }
                    }
                };

                //TODO: Obfuscate the email
                var response = await _sesClient.SendEmailAsync(request);
                _logger.LogInformation("Email sent successfully to {Email} with MessageId: {MessageId}",
                    email, response.MessageId);
            }
            catch (Exception ex)
            {
                //TODO: Obfuscate the email
                _logger.LogError(ex, "Failed to send email to {Email} using template {TemplateName}",
                    email, templateName);
                throw;
            }
        }

        private async Task<EmailTemplate> LoadTemplateAsync(string templateName)
        {
            var templateDirectory = Path.Combine(_templatesPath, templateName);

            if (!Directory.Exists(templateDirectory))
            {
                throw new FileNotFoundException($"Email template directory not found: {templateDirectory}");
            }

            var htmlFile = Path.Combine(templateDirectory, "template.html");
            var subjectFile = Path.Combine(templateDirectory, "subject.txt");

            var template = new EmailTemplate();

            if (File.Exists(htmlFile))
            {
                template.Body = await File.ReadAllTextAsync(htmlFile);
            }

            if (File.Exists(htmlFile))
            {
                template.Subject = await File.ReadAllTextAsync(subjectFile);
            }

            return template;
        }

        private EmailTemplate ProcessTemplate(EmailTemplate template, Dictionary<string, string> parameters)
        {
            var processedTemplate = new EmailTemplate
            {
                Subject = ProcessString(template.Subject, parameters),
                Body = ProcessString(template.Body, parameters),
            };

            return processedTemplate;
        }

        private string ProcessString(string input, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(input) || parameters == null)
                return input;

            var result = input;
            foreach (var parameter in parameters)
            {
                result = result.Replace($"{{{{{parameter.Key}}}}}", parameter.Value);
            }

            return result;
        }

        private class EmailTemplate
        {
            public string Subject { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
        }
    }
}
