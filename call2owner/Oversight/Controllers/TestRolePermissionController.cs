using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Utilities;

namespace Oversight.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TestRolePermissionController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("anonymous-dashboard")]
        public IActionResult GetAnonymousDashboard()
        {
            return Ok("Admin Dashboard Access Granted!");
        }

        [Authorize(Policy = Module.User)]
        [Authorize(Policy = Permission.Add)]
        [HttpGet("User_Add")]
        public IActionResult User_Add()
        {
            return Ok("You have permission to User_Add.");
        }

        [Authorize(Policy = Module.Quotation)]
        [Authorize(Policy = Permission.Add)]
        [HttpGet("Quotation_Add")]
        public IActionResult Quotation_Add()
        {
            return Ok("You have permission to User_Add.");
        }

        [Authorize(Policy = Module.Quotation)]
        [Authorize(Policy = Permission.Update)]
        [HttpGet("Quotation_Update")]
        public IActionResult Quotation_Update()
        {
            return Ok("You have permission to User_Update.");
        }

        [EmailTrigger(Module.Quotation, Permission.Update)]
        [Authorize(Policy = Module.Quotation)]
        [Authorize(Policy = Permission.Update)]
        [HttpGet("test-email")]
        public IActionResult TestEmail()
        {
            var user = new Model.User
            {
                FirstName = "model.FirstName",
                LastName = "model.LastName",
                Email = "caddycoder@gmail.com",
                MobileNumber = "9023937303",
                RoleId = 1,
                VerificationCode = "123-123-123",
                ParentRoleId = 1,
                VerificationCodeGenerationTime = DateTime.UtcNow,
                IsActive = true,
                IsVerified = false
            };

            string jsonVariables = JsonConvert.SerializeObject(user);

            var recipientEmail = Helper.ExtractMatchingValues(jsonVariables);

            HttpContext.Items["RecipientEmail"] = recipientEmail;
            //HttpContext.Items["EmailCc"] = new List<string> { "rahul.chouhan@kindlebit.org", "anjaligaur.developer@gmail.com" };
            //HttpContext.Items["RecipientBcc"] = new List<string> { "yogesh.kumar@kindlebit.org" };

            HttpContext.Items["VariablesRaw"] = jsonVariables;

            return Ok("Quotation - Update completed & email sent");
        }
    }
}
