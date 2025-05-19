using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Utilities;

namespace Oversight.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaypalWebhookController : ControllerBase
    {
        private readonly ILogger<PaypalWebhookController> _logger;

        public PaypalWebhookController(ILogger<PaypalWebhookController> logger)
        {
            _logger = logger;
        }

        [HttpPost("success")]
        public async Task<IActionResult> WebhookSuccess()
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var body = await reader.ReadToEndAsync();

                //_logger.LogInformation("Webhook received:\n{0}", body);

                ServerLogger.Log("success Case  ::");
                ServerLogger.Log("Payload:\n" + body);
                

                return Ok(); 
            }
        }

       
        [HttpPost("failure")]
        public async Task<IActionResult> WebhookFailure()
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var body = await reader.ReadToEndAsync();

                //_logger.LogError("Webhook received:\n{0}", body);
                ServerLogger.Log("fail  case.");
                ServerLogger.Log("Payload:\n" + body);


                return Ok(); 
            }
        }
    }
}
