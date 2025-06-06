using BizStreamAIAssistant.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace WebIndexingScheduler;

public class WebIndexingSchedulerFunction
{
    private readonly ILogger<WebIndexingSchedulerFunction> _logger;
    private readonly TextEmbeddingService _textEmbeddingService;
    private readonly WebIndexingService _webIndexingService;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public WebIndexingSchedulerFunction(
        ILogger<WebIndexingSchedulerFunction> logger,
        TextEmbeddingService textEmbeddingService,
        WebIndexingService webIndexingService
    )
    {
        if (textEmbeddingService == null)
        {
            throw new ArgumentNullException(nameof(textEmbeddingService));
        }

        if (webIndexingService == null)
        {
            throw new ArgumentNullException(nameof(webIndexingService));
        }
        _textEmbeddingService = textEmbeddingService;
        _webIndexingService = webIndexingService;
        _logger = logger;
    }

    [Function("BizStreamWebIndexingFunction")]
    public async Task Run(
        [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, // Run every 5 minute for testing
        // [TimerTrigger("0 * * * * *")] TimerInfo myTimer, // Run every minute for testing
        // [TimerTrigger("0 0 0 * * 1")] TimerInfo myTimer, // Run every Monday at midnight
        FunctionContext context)
    {
        if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(1)))
        {
            _logger.LogInformation("Another instance is already running. Skipping this execution.");
            return;
        }

        try
        {
            var startTime = DateTime.Now;
            _logger.LogInformation($"WebIndexingSchedulerFunction starting at: {startTime}");
            _logger.LogInformation($"Timer schedule status: Last={myTimer.ScheduleStatus?.Last}, Next={myTimer.ScheduleStatus?.Next}");

            _logger.LogInformation("Starting Websites Crawling and Data Extracting...");
            await _webIndexingService.CrawlAndExtractAsync();
            _logger.LogInformation("Websites Crawling and Data Extracting completed successfully.");

            _logger.LogInformation("Starting Text Embeddings Upload...");
            await _textEmbeddingService.UploadEmbeddingsAsync();
            _logger.LogInformation("Text Embeddings Upload completed successfully.");

            var endTime = DateTime.Now;
            _logger.LogInformation($"WebIndexingSchedulerFunction completed successfully. Duration: {(endTime - startTime).TotalSeconds:F2} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during web content indexing.");
            throw; // Rethrow to ensure the function is marked as failed
        }
        finally
        {
            _semaphore.Release();
        }
    }
}