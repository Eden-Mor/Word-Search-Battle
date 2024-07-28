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


        public async Task RemoveGameSessionChildren(GameSession gameSession, CancellationToken cancellationToken)
        {
            GameSessions.Remove(gameSession);
            await PlayerGameSessions
                    .Where(x => x.GameSessionId == gameSession.GameSessionId)
                    .ForEachAsync(x => PlayerGameSessions.Remove(x), cancellationToken);

            await WordList
                    .Where(x => x.GameSessionId == gameSession.GameSessionId)
                    .ForEachAsync(x => WordList.Remove(x), cancellationToken);

            await SaveChangesAsync(cancellationToken);
        }
    }
}
