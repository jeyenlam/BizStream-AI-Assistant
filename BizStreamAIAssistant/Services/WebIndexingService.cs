using System.Text;
using System.Text.Json;

namespace BizStreamAIAssistant.Services
{
    public class WebIndexingService
    {
        private readonly WebCrawler _webCrawler;
        public WebIndexingService()
        {
            _webCrawler = new WebCrawler();
        }
        public async Task<string> CrawlAndExtractAsync(string rootUrl, int depth = 1)
        {
            var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
            string jsonlFile = Path.Combine(projectRoot!, "Data", "Chunks", "data.jsonl");
            var filePath = Path.Combine(projectRoot!, "Data", "Raw");
            var crawlLogFilePath = Path.Combine(projectRoot!, "Data");
            var crawLogFileName = "CrawlLog.txt";
            var pages = await _webCrawler.CrawlAsync(rootUrl, depth);
            var totalChars = 0;

            File.WriteAllText(jsonlFile, string.Empty);

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var html = page.Item1;
                var url = page.Item2;
                var content = WebIndexingHelper.ExtractHtmlMainContent(html, url);
                var firstLine = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? $"NaN_{i}";
                var title = string.Concat(firstLine
                    .Where(c => !Path.GetInvalidFileNameChars().Contains(c)))
                    .Trim();
                title = title.Length > 50 ? title.Substring(0, 50) : title;
                WebIndexingHelper.ConvertTxtToJsonl(title, content, url);
                totalChars += content.ToString().Length;
            }

            File.AppendAllText(Path.Combine(crawlLogFilePath, crawLogFileName), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] - Depth: {depth}, Crawled {pages.Count} pages, {totalChars} total characters\n");
            return "Finished Indexing";
        }
    }
}