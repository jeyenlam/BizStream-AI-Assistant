namespace BizStreamAIAssistant.Helpers
{
    public static class TempDataPathConfig
    {
        private static string GetTempDataDirectory()
        {
            var baseDir = AppContext.BaseDirectory;
            var projectDir = Directory.GetParent(baseDir)?.Parent?.Parent?.FullName; // Go up to the project root directory
            if (projectDir == null)
            {
                throw new InvalidOperationException("Could not determine project directory");
            }
            
            var tempDataDir = Path.Combine(projectDir, "TempData");
            if (!Directory.Exists(tempDataDir))
            {
                Directory.CreateDirectory(tempDataDir);
            }
            
            return tempDataDir;
        }

        private static readonly string TempDataDirectory = GetTempDataDirectory();
        public static readonly string JsonlFilePath = Path.Combine(TempDataDirectory, "data.jsonl");
        public static readonly string CrawlLogFilePath = Path.Combine(TempDataDirectory, "CrawlLog.txt");
    }
}