using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Context
{
    namespace Infrastructure.Context
    {
        public class GameContextFactory : IDesignTimeDbContextFactory<GameContext>
        {
            public GameContext CreateDbContext(string[] args)
            {
                // 1. Find miljøet (Development eller Production)
                string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

                // 2. Find stien til Game-projektet for at læse appsettings
                string currentDirectory = Directory.GetCurrentDirectory();
                string gameProjectPath = Path.GetFullPath(Path.Combine(currentDirectory, "..", "Game"));

                if (currentDirectory.EndsWith("Game"))
                {
                    gameProjectPath = currentDirectory;
                }

                // 3. Konfigurations-setup
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(gameProjectPath)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                var optionsBuilder = new DbContextOptionsBuilder<GameContext>();
                var connectionString = configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception($"GameContextFactory: Forbindelsesstrengen 'DefaultConnection' blev ikke fundet for: {environmentName}");
                }

                // 4. Vi bruger nu udelukkende SQL Server (LocalDB eller Azure)
                optionsBuilder.UseSqlServer(connectionString);

                Console.WriteLine($"[EF-FACTORY] Genererer migration til SQL SERVER (Miljø: {environmentName})");

                return new GameContext(optionsBuilder.Options);
            }
        }
    }
}