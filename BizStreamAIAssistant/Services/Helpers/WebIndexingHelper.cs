using System.Net;
using HtmlAgilityPack;
using System.Text.Json;
// using BizStreamAIAssistant.Helpers;
using BizStreamAIAssistant.Models;
using BizStreamAIAssistant.Helpers;
// using System.Net.Http;
// using System.Threading.Tasks;

namespace BizStreamAIAssistant.Services.Helpers
{
    public class WebIndexingHelper
    {
        private static readonly HttpClient _httpClient = new();

        public static async Task<List<(string, string)>> CrawlAsync(string rootUrl, int depth)
        {
            string htmlPageContentFilePath = TempDataPathConfig.htmlPageContentFilePath;
            var pages = new List<(string Html, string Url)>();
            var toVisit = new Queue<(string Url, int level)>();
            var visitedUrls = new HashSet<string>();  // Create new HashSet for each crawl
            toVisit.Enqueue((rootUrl, 0));

            var count = 0;
            while (toVisit.Count > 0)
            {
                var (url, level) = toVisit.Dequeue();
                if (level > depth || visitedUrls.Contains(url))
                {
                    continue;
                }
                visitedUrls.Add(url);

                try
                {
                    var html = await _httpClient.GetStringAsync(url);
                    pages.Add((html, url));

                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var rootTrimmed = rootUrl.TrimEnd('/');
                    var wwwPrefix = rootUrl.Insert(8, "www.");
                    var links = doc.DocumentNode.SelectNodes("//a[@href]")?
                        .Select(node => node.GetAttributeValue("href", "").Trim())
                        .Where(href =>
                        {
                            if (string.IsNullOrWhiteSpace(href)) return false;

                            var lower = href.ToLowerInvariant();
                            if (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") ||
                                lower.EndsWith(".png") || lower.EndsWith(".svg"))
                                return false;

                            return href.StartsWith(rootUrl, StringComparison.OrdinalIgnoreCase) ||
                                href.StartsWith(wwwPrefix, StringComparison.OrdinalIgnoreCase) ||
                                href.StartsWith("/");
                        })
                        .Select(href =>
                        {
                            // Normalize link
                            href = href.Replace("www.", "");

                            return href.StartsWith("/")
                                ? rootTrimmed + href
                                : href;
                        })
                        .Distinct()
                        .ToList();

                    Console.WriteLine($"{++count} Crawling: {url} at level {level}, found {links?.Count ?? 0} links.");

                    if (links == null)
                    {
                        throw new Exception("No link found.");
                    }

                    foreach (var link in links)
                    {
                        toVisit.Enqueue((link, level + 1));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to crawl: {url} \nError: {e.Message}");
                    continue;
                }
            }

            FileHelper.EmptyFile(htmlPageContentFilePath); // Comment out when not testing
            File.WriteAllText(htmlPageContentFilePath, string.Join(Environment.NewLine, pages)); // Comment out when not testing
            return pages;
        }

        public static string ExtractPageContent(string html, string url)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var content = doc.DocumentNode
                .SelectNodes("//p | //h1 ") //| //h2 | //h3 | //h4 | //h5 | //h6
                ?.Select(n => WebUtility.HtmlDecode(n.InnerText.Trim()))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                ?? Enumerable.Empty<string>();
            
            return string.Join($"\n", content);
        }

        public static void WritePageContentToFile(string jsonlFilePath, PageContentModel pageContent)
        {
            using var writer = new StreamWriter(jsonlFilePath, append: true);
            var (id, pageTitle, content, url) = pageContent;

            // Convert each line of content into a JSON object and write to the file
            // int i = 1;
            // foreach (var textChunk in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            // {
            //     if (textChunk.ToString().Length < 100)
            //     {
            //         continue;
            //     }

            //     var jsonObject = new
            //     {
            //         id = $"p{id}_l{i++}",
            //         pageTitle,
            //         url,
            //         text = textChunk,
            //     };

            //     writer.WriteLine(JsonSerializer.Serialize(jsonObject));
            // }

            var jsonObject = new
            {
                id,
                pageTitle,
                url,
                text = content,
            };

            writer.WriteLine(JsonSerializer.Serialize(jsonObject));
        }
    }
}
