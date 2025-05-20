namespace BizStreamWebsiteClone.Models
{
    public class NavSubItem
    {
        public required string Name { get; set; }
        public required string Url { get; set; }
    }
    public class NavItems
    {
        public required string Name { get; set; }
        public required string Url { get; set; }
        public required bool Collapsable { get; set; }
        public NavSubItem[] SubItems { get; set; }
    }
}