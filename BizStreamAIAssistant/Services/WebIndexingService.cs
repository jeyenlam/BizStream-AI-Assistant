// using System.Text;
// using System.Text.Json;
using BizStreamAIAssistant.Helpers;
using BizStreamAIAssistant.Models;
using BizStreamAIAssistant.Services.Helpers;
using Microsoft.Extensions.Options;

namespace BizStreamAIAssistant.Services
{
    public class WebIndexingService
    {
        private readonly WebIndexingSettingsModel _webIndexingSettings;

        public WebIndexingService(IOptions<WebIndexingSettingsModel> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _webIndexingSettings = options.Value;
            if (_webIndexingSettings.RootUrl == null || _webIndexingSettings.Depth == 0)
            {
                throw new InvalidOperationException("WebIndexingSettings.RootUrl or WebIndexingSettings.Depth is not configured properly.");
            }
        }
        
        public async Task<string> CrawlAndExtractAsync()
        {
            // 1. Crawl the websites, return html doc of pages
            string rootUrl = _webIndexingSettings.RootUrl;
            int depth = _webIndexingSettings.Depth;

            string jsonlFilePath = TempDataPathConfig.JsonlFilePath;
            string crawlLogFilePath = TempDataPathConfig.CrawlLogFilePath;
            var pages = await WebIndexingHelper.CrawlAsync(rootUrl, depth);

            // 2. Empty out data.jsonl before writing data to it
            FileHelper.EmptyFile(jsonlFilePath);
            FileHelper.EmptyFile(TempDataPathConfig.cleanedPageContentFilePath);


            // 3. Start writing extracted data to data.jsonl
            var totalChars = 0;
            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var html = page.Item1;
                var url = page.Item2;
                var content = WebIndexingHelper.ExtractPageContent(html, url);
                var pageTitle = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? $"NaN_{i}";

                pageTitle = new string(pageTitle
                    .Where(c => !Path.GetInvalidFileNameChars().Contains(c))
                    .ToArray())
                    .Trim();

                pageTitle = pageTitle.Length > 30
                    ? string.Concat(pageTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(word => char.ToUpper(word[0])))
                    : pageTitle;

                File.AppendAllText(TempDataPathConfig.cleanedPageContentFilePath, content + Environment.NewLine +  url + Environment.NewLine + Environment.NewLine);

                var pageContent = new PageContentModel
                {
                    Id = (i + 1).ToString(),
                    PageTitle = pageTitle,
                    Content = content,
                    Url = url
                };

                WebIndexingHelper.WritePageContentToFile(jsonlFilePath, pageContent);
                totalChars += content.ToString().Length;
            }

            // 4. Keep track of crawling
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] - Depth: {depth}, Crawled {pages.Count} pages, {totalChars} total characters\n";
            File.AppendAllText(crawlLogFilePath, logMessage);
            return jsonlFilePath;
        }
    }
}