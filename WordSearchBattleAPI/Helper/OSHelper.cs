namespace WordSearchBattleAPI.Helper
{
    public static class OSHelper
    {
        public static bool IsWindows() => Environment.OSVersion.Platform == PlatformID.Win32NT;
        public static bool IsLinux() => Environment.OSVersion.Platform == PlatformID.Unix;
    }
}
