using Microsoft.AspNetCore.Mvc;
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
            Console.WriteLine($"Request: {request}");
            if (request?.Messages == null || !request.Messages.Any())
            {
                return BadRequest("No messages provided");
            }

            var response = await _chatbotService.GetResponseAsync(request.Messages);
            return Ok(new {text = response});
        }
    }
}