namespace WordSearchBattleAPI.Helper
{
    public static class ConsoleLog
    {
        public static void WriteLine(string message)
            => Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] {message}");
    }
}
