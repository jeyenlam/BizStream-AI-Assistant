using System.Threading.Tasks;
using BizStreamAIAssistant.Models;

namespace BizStreamAIAssistant.Services
{
    public interface IChatbotService
    {
        Task<string> GetResponseAsync(List<Message> message);
    }
}
