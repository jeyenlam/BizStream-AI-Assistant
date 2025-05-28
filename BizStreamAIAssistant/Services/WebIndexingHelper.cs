using System.Net;
using HtmlAgilityPack;
using System.Text.Json;
using System.Threading.Tasks;

namespace BizStreamAIAssistant.Services
{
    public static class WebIndexingHelper
    {
        public static string ExtractHtmlMainContent(string html, string url)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var content = doc.DocumentNode
                .SelectNodes("//p | //h1 | //h2 | //h3 | //h4 | //h5 | //h6")
                ?.Select(n => WebUtility.HtmlDecode(n.InnerText.Trim()))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                ?? Enumerable.Empty<string>();

            return string.Join($"\n", content);
        }

        public static void ConvertTxtToJsonl(string title, string content, string url)
        {
            var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
            string jsonlFile = Path.Combine(projectRoot!, "Data", "Chunks", "data.jsonl");
            using var writer = new StreamWriter(jsonlFile, append: true);

            foreach (var line in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.ToString().Length < 30)
                {
                    continue;
                }

                var jsonObject = new
                {
                    title = title,
                    url = url,
                    content = line,
                };

                string jsonLine = JsonSerializer.Serialize(jsonObject);
                writer.WriteLine(jsonLine);
            }
        }
    }
}
