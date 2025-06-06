namespace BizStreamAIAssistant.Helpers
{
    public static class PathConfig
    {
        // public static readonly string ProjectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName!;
        private static string GetProjectRoot()
        {
            var baseDir = AppContext.BaseDirectory;
            
            // First try to find the Data directory in the current directory
            var dataDir = Path.Combine(baseDir, "Data");
            if (!Directory.Exists(dataDir))
            {
                // If not found, create it in the base directory
                Directory.CreateDirectory(dataDir);
            }
            
            return baseDir;
        }

        private static readonly string ProjectRoot = GetProjectRoot();
        private static readonly string DataDirectory = Path.Combine(ProjectRoot, "Data");
        
        public static readonly string JsonlFilePath = Path.Combine(DataDirectory, "Chunks", "data.jsonl");
        public static readonly string RawDataPath = Path.Combine(DataDirectory, "Raw");
        public static readonly string CrawlLogFilePath = Path.Combine(DataDirectory, "CrawlLog.txt");

        static PathConfig()
        {
            // Ensure directories exist
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(JsonlFilePath)!);
            Directory.CreateDirectory(RawDataPath);
            
            // Create the log file if it doesn't exist
            if (!File.Exists(CrawlLogFilePath))
            {
                File.WriteAllText(CrawlLogFilePath, "");
            }
        }
    }
}