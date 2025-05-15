using System.Threading.Tasks;

namespace BizStreamAIAssistant.Services
{
    public interface IChatbotService
    {
        Task<string> GetResponseAsync(string message);
    }
}
