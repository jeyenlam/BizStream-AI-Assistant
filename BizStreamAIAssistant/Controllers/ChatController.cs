using Microsoft.AspNetCore.Mvc;
using BizStreamAIAssistant.Models;
using BizStreamAIAssistant.Services;

namespace BizStreamAIAssistant.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;
        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Chatbot API is running");
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestModel request)
        {
            Console.WriteLine($"Request: {request.Messages.Last().Content}");
            if (request?.Messages == null || !request.Messages.Any())
            {
                return BadRequest("No messages provided");
            }

            var response = await _chatService.GetResponseTestAsync(request.Messages);
            return Ok(new {text = response});
        }
    }
}