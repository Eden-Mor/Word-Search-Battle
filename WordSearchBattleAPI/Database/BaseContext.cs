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
            var useLocalDB = false;
            var host = OSHelper.IsLinux()
                ? Configuration.GetSection("Database")["DockerDBHost"]
                : useLocalDB
                    ? "localhost"
                    : Configuration.GetSection("Database")["DirectServerDBHost"];

            var port = OSHelper.IsLinux() || useLocalDB
                ? Configuration.GetSection("Database")["LocalServerPort"]
                : Configuration.GetSection("Database")["RemoteServerPort"];


            //Connect to postgres with connection string from app settings
            var connectionString = Configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("Connection String missing.");

            connectionString = connectionString.Replace("{HOST}", host)
                                               .Replace("{PORT}", port)
                                               .Replace("{DB_USERNAME}", DockerSecretHelper.ReadSecret("postgres-u"))
                                               .Replace("{DB_PASSWORD}", DockerSecretHelper.ReadSecret("postgres-p"));

            optionsBuilder.UseNpgsql(connectionString, builder =>
            {
                builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
        }
    }
}
