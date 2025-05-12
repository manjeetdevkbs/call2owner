using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace Utilities
{
    public interface IEmailServiceClient
    {
        Task<RegisterEmailMessageDto> CheckEmailRegistrationAsync(int moduleId, int permissionId);
        Task<EmailTemplateDto> GetTemplateAsync(int templateId);
        Task<EmailTemplateDto?> GetTemplateAsync();
        Task<bool> SendEmailAsync(EmailMessageOutputDto emailDto);
    }

    public class EmailServiceClient : IEmailServiceClient
    {
        private readonly RestClient _client;
        private readonly IConfiguration _config;
        private readonly ILogger<EmailServiceClient> _logger;

        public EmailServiceClient(IConfiguration config, ILogger<EmailServiceClient> logger)
        {
            _config = config;
            _logger = logger;
            _client = new RestClient(config["EmailService:BaseUrl"]);
        }

        public async Task<RegisterEmailMessageDto> CheckEmailRegistrationAsync(int moduleId, int permissionId)
        {
            try
            {
                var request = new RestRequest("RegisterEmail/check-registration")
                    .AddQueryParameter("moduleId", moduleId)
                    .AddQueryParameter("permissionId", permissionId);

                var response = await _client.ExecuteGetAsync<RegisterEmailMessageDto>(request);

                if (response.IsSuccessful && response.Data != null)
                    return response.Data;

                _logger.LogWarning("Failed to fetch RegisterEmailMessage: StatusCode={StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting RegisterEmailMessage");
                return null;
            }
        }

        public async Task<EmailTemplateDto> GetTemplateAsync(int templateId)
        {
            try
            {
                var request = new RestRequest($"Email/get-template/{templateId}");
                var response = await _client.ExecuteGetAsync<EmailTemplateDto>(request);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email template");
                return null;
            }
        }

        public async Task<EmailTemplateDto?> GetTemplateAsync()
        {
            try
            {
                var request = new RestRequest("api/email/templates/1");
                var response = await _client.ExecuteGetAsync<EmailTemplateDto>(request);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email template");
                return null;
            }
        }

        public async Task<bool> SendEmailAsync(EmailMessageOutputDto emailDto)
        {
            try
            {
                //https://localhost:7260/TestRolePermission/test-email
                //https://localhost:7280/TestAction/send-email
                var request = new RestRequest($"TestAction/send-email")
                    .AddJsonBody(emailDto);

                var response = await _client.ExecutePostAsync(request);
                return response.IsSuccessful;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return false;
            }
        }
    }
}
