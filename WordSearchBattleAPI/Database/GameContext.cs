using Microsoft.EntityFrameworkCore;
using WordSearchBattleAPI.Helper;
using WordSearchBattleAPI.Models;

namespace WordSearchBattleAPI.Database
{
    public class GameContext(IConfiguration configuration) : BaseContext(configuration)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var schemaName = Configuration.GetSection("Database")["GameSchemaName"];
            modelBuilder.HasDefaultSchema(schemaName);
        }

        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<PlayerGameSession> PlayerGameSessions { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<WordListItem> WordList { get; set; }


        public async Task DeletePlayerGameSessionAsync(GameSession gameSession, CancellationToken cancellationToken)
        {
            var playerGameSessions = PlayerGameSessions.Where(x => x.GameSessionId == gameSession.GameSessionId);
            PlayerGameSessions.RemoveRange(playerGameSessions);
            await SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteWordDataAsync(GameSession gameSession, CancellationToken cancellationToken)
        {
            var wordListEntries = WordList.Where(x => x.GameSessionId == gameSession.GameSessionId);
            WordList.RemoveRange(wordListEntries);
            await SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteGameSessionAsync(GameSession gameSession, CancellationToken cancellationToken)
        {
            GameSessions.Remove(gameSession);
            await SaveChangesAsync(cancellationToken);
        }


        public async Task RemoveGameSessionChildren(GameSession gameSession, CancellationToken cancellationToken)
        {
            await DeletePlayerGameSessionAsync(gameSession, cancellationToken);
            await DeleteWordDataAsync(gameSession, cancellationToken);
            await DeleteGameSessionAsync(gameSession, cancellationToken);
        }
    }
}
