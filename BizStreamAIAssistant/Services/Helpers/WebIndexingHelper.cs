using System.Net;
using HtmlAgilityPack;
using System.Text.Json;
using BizStreamAIAssistant.Models;
using BizStreamAIAssistant.Helpers;

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

            // FileHelper.EmptyFile(htmlPageContentFilePath);
            // File.WriteAllText(htmlPageContentFilePath, string.Join(Environment.NewLine, pages));
            return pages;
        }

        public static (string, string) ExtractPageTitleAndContent(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var raw = doc.DocumentNode
                .SelectNodes("//p | //h1")
                ?.Select(n => WebUtility.HtmlDecode(n.InnerText.Trim()))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList()
                ?? new List<string>();

            string pageTitle = raw.FirstOrDefault() ?? "NaN";
            string pageContent = string.Join("\n", raw.Skip(1).SkipLast(2)); // skip first line (title), last two lines (call to action that appears on every page)

            return (pageTitle, pageContent);
        }


        public static void WritePageContentToFile(string jsonlFilePath, PageContentModel pageContent)
        {
            using var writer = new StreamWriter(jsonlFilePath, append: true);
            var (id, pageTitle, content, url) = pageContent;

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
