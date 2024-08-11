namespace WordSearchBattleAPI.Helper
{
    public static class DockerSecretHelper
    {
        public static string ReadSecret(string fileName)
        {
            string directory = GetDirectoryOfSecret();
            return ReadSecret(directory, fileName);
        }

        private static string GetDirectoryOfSecret()
        {
            if (OSHelper.IsLinux())
                return "/run/secrets";
            else if (OSHelper.IsWindows())
                return GetWindowsDirectory();
            else //We dont have a mac to test with, cant implement it without testing it.
                throw new NotSupportedException("Mac OS not supported for secret retrieval.");
        }

        private static string GetWindowsDirectory()
        {
            string currentDirectory = Directory.GetCurrentDirectory() ?? throw new DirectoryNotFoundException("Current folder not found.");
            string parentDirectory = Directory.GetParent(currentDirectory)?.FullName ?? throw new DirectoryNotFoundException("Parent folder not found.");
            return parentDirectory;
        }

        private static string ReadSecret(string directory, string fileName)
        {
            try
            {
                // Combine directory and file name to get the full path
                string filePath = Path.Combine(directory, fileName + (OSHelper.IsWindows() ? ".txt" : string.Empty));

                // Check if the file exists
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Secret file not found - {filePath}", filePath);

                // Read the contents of the file
                string secret = File.ReadAllText(filePath);

                return secret;
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteLine($"An Exception occurred while trying to read the secret from directory '{directory}' with filename '{fileName}'. Error is: '{ex.Message}'");
                return string.Empty;
            }
        }
    }
}
