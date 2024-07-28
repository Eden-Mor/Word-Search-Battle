using Microsoft.EntityFrameworkCore;
using WordSearchBattleAPI.Helper;
using WordSearchBattleAPI.Models;

namespace WordSearchBattleAPI.Database
{
    public class BaseContext(IConfiguration configuration) : DbContext
    {
        protected readonly IConfiguration Configuration = configuration;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var useLocalDB = true;
            var host = OSHelper.IsLinux()
                    ? Configuration.GetSection("Database")["DockerDBHost"]
                    : useLocalDB
                        ? "localhost"
                        : Configuration.GetSection("Database")["DirectServerDBHost"];


            //Connect to postgres with connection string from app settings
            var connectionString = Configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("Connection String missing.");

            connectionString = connectionString.Replace("{HOST}", host)
                                               .Replace("{DB_USERNAME}", DockerSecretHelper.ReadSecret("postgres-u"))
                                               .Replace("{DB_PASSWORD}", DockerSecretHelper.ReadSecret("postgres-p"));

            optionsBuilder.UseNpgsql(connectionString, builder =>
            {
                builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
        }
    }
}
