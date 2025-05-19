using Newtonsoft.Json;
using Utilities;

namespace Oversight
{
    public class EmailTriggerService : IEmailTriggerService
    {
        private readonly IEmailServiceClient _emailClient;
        private readonly ILogger<EmailTriggerService> _logger;

        public EmailTriggerService(IEmailServiceClient emailClient, ILogger<EmailTriggerService> logger)
        {
            _emailClient = emailClient;
            _logger = logger;
        }

        public async Task TriggerEmailIfConfigured(int moduleId, int permissionId,
            string recipientEmail, string variables, 
            List<string>? attachmentUrls, List<string>? emailCc, List<string>? emailBcc)
        {
            try
            {
                // 1. Check registration
                var registeredResponse = await _emailClient.CheckEmailRegistrationAsync(moduleId, permissionId);
                if (registeredResponse == null) return;

                // 2. Get template (you might need to store templateId somewhere)
                var template = await _emailClient.GetTemplateAsync(registeredResponse.TemplateId);

                if (template == null) return;

                // 3. Send email
                var emailRequest = new EmailMessageOutputDto
                {
                    TemplateId = template.Id,
                    RecipientEmail = recipientEmail, 
                    EmailCc = emailCc,
                    EmailBcc = emailBcc,
                    Variables = variables,
                    VariablesRaw = variables,
                    Attachments = attachmentUrls
                };

                await _emailClient.SendEmailAsync(emailRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering email");
            }
        }
    }

    public interface IEmailTriggerService
    {
        Task TriggerEmailIfConfigured(int moduleId, int permissionId,
            string recipientEmail, string variables,
            List<string>? attachmentUrls, List<string>? emailCc, List<string>? emailBcc);
    }
}
