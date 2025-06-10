using BizStreamAIAssistant.Helpers;
using BizStreamAIAssistant.Models;
using BizStreamAIAssistant.Services.Helpers;
using Microsoft.Extensions.Options;

namespace BizStreamAIAssistant.Services
{
    public class WebIndexingService
    {
        private readonly WebIndexingSettingsModel _webIndexingSettings;
        private const int MaxChunkSize = 8000; // OpenAI's text-embedding-ada-002 has an 8k token limit
        private readonly string jsonlFilePath = TempDataPathConfig.JsonlFilePath;
        private readonly string crawlLogFilePath = TempDataPathConfig.CrawlLogFilePath;

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
            string rootUrl = _webIndexingSettings.RootUrl;
            int depth = _webIndexingSettings.Depth;
            var pages = await WebIndexingHelper.CrawlAsync(rootUrl, depth);

            FileHelper.EmptyFile(jsonlFilePath);
            FileHelper.EmptyFile(TempDataPathConfig.cleanedPageContentFilePath);

            var totalChars = 0;
            var chunkId = 1;
            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var html = page.Item1;
                var url = page.Item2;
                var pageTitleAndContent = WebIndexingHelper.ExtractPageTitleAndContent(html);
                string pageTitle = pageTitleAndContent.Item1;
                string content = pageTitleAndContent.Item2;

                File.AppendAllText(
                    TempDataPathConfig.cleanedPageContentFilePath,
                    pageTitle + Environment.NewLine
                    + content + Environment.NewLine
                    + url + Environment.NewLine
                    + Environment.NewLine);

                var chunks = ChunkContent(content);
                foreach (var chunk in chunks)
                {
                    if (string.IsNullOrWhiteSpace(chunk)) continue;

                    var pageContent = new PageContentModel
                    {
                        Id = chunkId.ToString(),
                        PageTitle = pageTitle,
                        Content = chunk,
                        Url = url
                    };

                    WebIndexingHelper.WritePageContentToFile(jsonlFilePath, pageContent);
                    totalChars += chunk.Length;
                    chunkId++;
                }
            }

            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] - Depth: {depth}, Crawled {pages.Count} pages, {totalChars} total characters\n";
            File.AppendAllText(crawlLogFilePath, logMessage);
            return jsonlFilePath;
        }

        private List<string> ChunkContent(string content)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(content)) return chunks;

            // Split into paragraphs first
            var paragraphs = content.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            var currentChunk = new List<string>();
            var currentLength = 0;

            foreach (var paragraph in paragraphs)
            {
                // If adding this paragraph would exceed the limit, save current chunk and start new one
                if (currentLength + paragraph.Length > MaxChunkSize && currentChunk.Any())
                {
                    chunks.Add(string.Join("\n", currentChunk));
                    currentChunk.Clear();
                    currentLength = 0;
                }

                // If a single paragraph is longer than the limit, split it into sentences
                if (paragraph.Length > MaxChunkSize)
                {
                    var sentences = paragraph.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim() + ".");

                    foreach (var sentence in sentences)
                    {
                        if (currentLength + sentence.Length > MaxChunkSize && currentChunk.Any())
                        {
                            chunks.Add(string.Join("\n", currentChunk));
                            currentChunk.Clear();
                            currentLength = 0;
                        }
                        currentChunk.Add(sentence);
                        currentLength += sentence.Length + 1; // +1 for newline
                    }
                }
                else
                {
                    currentChunk.Add(paragraph);
                    currentLength += paragraph.Length + 1; // +1 for newline
                }
            }

            // Add any remaining content
            if (currentChunk.Any())
            {
                chunks.Add(string.Join("\n", currentChunk));
            }

            return chunks;
        }
    }
}