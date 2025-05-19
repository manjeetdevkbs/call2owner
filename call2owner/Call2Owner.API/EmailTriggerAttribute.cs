using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Utilities;

namespace Oversight
{
    public class EmailTriggerAttribute : ActionFilterAttribute
    {
        public string ModuleId { get; }
        public string PermissionId { get; }

        public EmailTriggerAttribute(string moduleId, string permissionId)
        {
            ModuleId = moduleId;
            PermissionId = permissionId;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Execute the action first
            var result = await next();

            if (result.Result is OkObjectResult okResult && okResult.Value != null)
            {
                var emailService = context.HttpContext.RequestServices.GetRequiredService<IEmailTriggerService>();

                var variables = new Dictionary<string, string>();
                var attachmentUrls = new List<string>();

                // Example: Get from HttpContext.Items or route data if set by action/controller
                var recipientEmail = context.HttpContext.Items["RecipientEmail"] as List<string>;
                var emailCc = context.HttpContext.Items["EmailCc"] as List<string>;
                var emailBcc = context.HttpContext.Items["RecipientBcc"] as List<string>;

                var variablesRaw = context.HttpContext.Items["VariablesRaw"] as string;

                if (recipientEmail!= null && recipientEmail.Count() > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        //public async Task TriggerEmailIfConfigured(int moduleId, int permissionId,
                        //string recipientEmail, Dictionary<string, string>? variables,
                        //List<string>? attachmentUrls, List<string>? emailCc, List<string>? recipientBcc)

                        await emailService.TriggerEmailIfConfigured(
                            int.Parse(ModuleId),
                            int.Parse(PermissionId),
                            recipientEmail != null ? string.Join(", ", recipientEmail) : string.Empty,
                            variablesRaw,
                            attachmentUrls,
                            emailCc,
                            emailBcc
                        );
                    });
                }
            }
        }
    }
}
