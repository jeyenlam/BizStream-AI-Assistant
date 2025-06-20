namespace BizStreamAIAssistant.Models;

public class PageContentModel
{
    public required string Id { get; set; }
    public required string PageTitle { get; set; }
    public required string Content { get; set; }
    public required string Url { get; set; }
    public void Deconstruct(out string id, out string pageTitle, out string content, out string url)
    {
        id = Id;
        pageTitle = PageTitle;
        content = Content;
        url = Url;
    }
}