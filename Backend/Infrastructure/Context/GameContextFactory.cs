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
                // Finder stien til Game-projektet
                string gameProjectPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Game"));

                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(gameProjectPath)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .Build();

                var optionsBuilder = new DbContextOptionsBuilder<GameContext>();
                var connectionString = configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("DefaultConnection connection string kunne ikke findes!");
                }

                // Fix for "to databaser" problemet: 
                // Hvis vi bruger SQLite lokalt, tvinger vi stien til altid at pege på Game-mappen
                if (connectionString.Contains(".db") || connectionString.Contains("Data Source"))
                {
                    string dbFileName = connectionString.Split('=')[1];
                    string absoluteDbPath = Path.Combine(gameProjectPath, dbFileName);
                    optionsBuilder.UseSqlite($"Data Source={absoluteDbPath}");
                }
                else
                {
                    optionsBuilder.UseSqlServer(connectionString);
                }

                return new GameContext(optionsBuilder.Options);
            }
        }
    }
}