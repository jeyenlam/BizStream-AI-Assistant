using HtmlAgilityPack;

namespace BizStreamAIAssistant.Services
{
    public static class HtmlExtractor
    {
        public static string ExtractMainContent(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var content = doc.DocumentNode
                .SelectNodes("//p | //h1 | //h2 | //h3 | //h4 | //h5 | //h6")
                ?.Select(n => n.InnerText.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                ?? Enumerable.Empty<string>();

            return string.Join("\n", content);
        }
    }
}
