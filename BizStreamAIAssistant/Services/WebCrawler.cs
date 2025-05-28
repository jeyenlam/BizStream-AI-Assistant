using System.Net.Http;
using HtmlAgilityPack;

namespace BizStreamAIAssistant.Services
{
    public class WebCrawler
    {
        private readonly HttpClient _httpClient = new();
        private readonly HashSet<string> _visitedUrls = new();

        public async Task<List<(string, string)>> CrawlAsync(string rootUrl, int depth)
        {
            var toVisit = new Queue<(string Url, int level)>();
            toVisit.Enqueue((rootUrl, 0));

            var pages = new List<(string Html, string Url)>();
            var count = 0;

            while (toVisit.Count > 0)
            {
                var (url, level) = toVisit.Dequeue();
                if (level > depth || _visitedUrls.Contains(url))
                {
                    continue;
                }
                _visitedUrls.Add(url);

                try
                {
                    var html = await _httpClient.GetStringAsync(url);
                    pages.Add((html, url));

                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var links = doc.DocumentNode.SelectNodes("//a[@href]")
                        ?.Select(node => node.GetAttributeValue("href", "").Trim())
                        .Where(href => href.StartsWith(rootUrl) || href.StartsWith(rootUrl.Insert(8, "www.")) || href.StartsWith('/'))
                        .Select(href => href.Replace("www.", ""))
                        .Select(href => href.StartsWith("/") ? rootUrl.TrimEnd('/') + href : href)
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
            File.WriteAllText($"Pages.txt", pages.Count > 0 ? string.Join(Environment.NewLine, pages) : "No pages crawled.");
            return pages;
        }
    }
}