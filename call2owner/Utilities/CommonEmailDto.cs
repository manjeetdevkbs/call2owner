using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Utilities
{
    public class RegisterEmailMessageDto
    {
        public int Id { get; set; }  // Primary Key
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public int ModuleId { get; set; }
        public int PermissionId { get; set; }
        public string ActionType { get; set; }
        public List<string> AttachmentFileName { get; set; } = new();
    }

    public class EmailTemplateInputDto
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool HasAttachment { get; set; }
        public List<string> Variables { get; set; }
        public List<string>? Attachments { get; set; }

        public bool SendInstantEmail { get; set; } = true;
        public string Duration { get; set; }
        public bool RecurringEmail { get; set; } = false;
    }

    public class EmailTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool HasAttachment { get; set; }
        public List<string> Variables { get; set; }
        public List<string>? Attachments { get; set; }

        public bool SendInstantEmail { get; set; } = true;
        public string Duration { get; set; }
        public bool RecurringEmail { get; set; } = false;
    }

    public class EmailMessageOutputDto
    {
        public int TemplateId { get; set; }
        public string RecipientEmail { get; set; }
        public List<string>? EmailCc { get; set; }
        public List<string>? EmailBcc { get; set; }

        [FromForm(Name = "Variables")]
        public string VariablesRaw { get; set; }
        public string Variables { get; set; }
        public bool HasAttachment { get; set; }

        //public List<string>? Attachments { get; set; }

        public List<string>? Attachments { get; set; }
        public bool SendInstantEmail { get; set; }
        public string? Duration { get; set; }
        public bool RecurringEmail { get; set; }
    }

    public class EmailMessageDto
    {
        public int TemplateId { get; set; }
        public string RecipientEmail { get; set; }

        [FromForm(Name = "Variables")] // Receive as string first
        public string VariablesRaw { get; set; }
        public Dictionary<string, string> Variables { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }
}
