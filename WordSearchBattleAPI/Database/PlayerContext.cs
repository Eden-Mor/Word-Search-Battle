using Microsoft.EntityFrameworkCore;
using WordSearchBattleAPI.Helper;
using WordSearchBattleAPI.Models;

namespace WordSearchBattleAPI.Database
{
    public class PlayerContext(IConfiguration configuration) : BaseContext(configuration)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var schemaName = Configuration.GetSection("Database")["PlayerSchemaName"];
            modelBuilder.HasDefaultSchema(schemaName);
        }

        public DbSet<User> Users { get; set; }
    }
}
