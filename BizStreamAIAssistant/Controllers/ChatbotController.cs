using Microsoft.AspNetCore.Mvc;
// using System.Threading.Tasks;
using BizStreamAIAssistant.Models;
using BizStreamAIAssistant.Services;

namespace BizStreamAIAssistant.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : Controller
    {

        // Dependency Injection
        // The constructor takes an IChatbotService instance, which is injected by the ASP.NET Core framework. 
        // ASP.NET needs a way to inject chatbot logic into the controller
        private readonly IChatbotService _chatbotService; // A private field to store the service instance.

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService; // stores the injected instance so can use it later in action methods.

        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Chatbot API is running");
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestModel request)
        {
            if (request == null || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Invalid request");
            }

            var response = await _chatbotService.GetResponseAsync(request.Message);
            return Ok(response);
        }
    }
}