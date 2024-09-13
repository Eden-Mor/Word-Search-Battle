using Microsoft.EntityFrameworkCore;
using WordSearchBattleAPI.Database;

namespace WordSearchBattleAPI.Services
{
    public class RoomCodeGeneratorService(IServiceProvider serviceProvider) : IRoomCodeGenerator
    {
        private static readonly Random Random = new();
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            GameContext gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

            string code;
            do
            {
                code = GenerateCode();
            } while (await DoesCodeExistAsync(code, gameContext, cancellationToken));

            return code;
        }

        private static string GenerateCode()
            => new(Enumerable.Repeat(Chars, 4)
                            .Select(s => s[Random.Next(s.Length)])
                            .ToArray());

        private async Task<bool> DoesCodeExistAsync(string code, GameContext gameContext, CancellationToken cancellationToken)
            => await gameContext.GameSessions.AnyAsync(e => e.RoomCode == code, cancellationToken);
    }

    public interface IRoomCodeGenerator
    {
        public Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken);
    }
}
