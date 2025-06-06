namespace BizStreamAIAssistant.Helpers
{
    public static class FileHelper
    {
        public static void EmptyFile(string filePath)
        {
            File.WriteAllText(filePath, string.Empty);
        }
    }
}