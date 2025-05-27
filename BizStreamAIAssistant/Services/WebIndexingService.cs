using System.Text;

namespace BizStreamAIAssistant.Services
{
    public class WebIndexingService
    {
        private readonly WebCrawler _webCrawler;
        public WebIndexingService()
        {
            _webCrawler = new WebCrawler();
        }

        public async Task<string> CrawlAndExtractAsync(string rootUrl, int depth = 2)
        {
            var extractedDataFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_ExtractedData.txt";
            var pages = await _webCrawler.CrawlAsync(rootUrl, depth);
            var allText = new StringBuilder();
            int pageCount = 1;

            foreach (var page in pages)
            {
                var text = HtmlExtractor.ExtractMainContent(page);
                allText.AppendLine($"Page {pageCount++}:{text}\n");
            }

            var totalChars = allText.ToString().Length;
            File.WriteAllText(extractedDataFileName, allText.ToString());
            File.AppendAllText("CrawlLog.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] - Crawled {pageCount} pages, {totalChars} total characters\n");

            return $"Finished Indexing: Extracted Data Stored at {extractedDataFileName}";       
        }
    }

}