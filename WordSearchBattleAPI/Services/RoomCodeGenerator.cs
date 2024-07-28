using Microsoft.EntityFrameworkCore;
using WordSearchBattleAPI.Database;

namespace WordSearchBattleAPI.Services
{
    public class RoomCodeGeneratorService(GameContext context) : IRoomCodeGenerator
    {
        private static readonly Random Random = new();
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
        {
            string code;
            do
            {
                code = GenerateCode();
            } while (await CodeExistsAsync(code, cancellationToken));

            return code;
        }

        private static string GenerateCode()
            => new(Enumerable.Repeat(Chars, 4)
                            .Select(s => s[Random.Next(s.Length)])
                            .ToArray());

        private async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
            => await context.GameSessions.AnyAsync(e => e.RoomCode == code, cancellationToken);
    }

    public interface IRoomCodeGenerator
    {
        public Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken);
    }
}
