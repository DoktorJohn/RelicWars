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
    public class GameContextFactory : IDesignTimeDbContextFactory<GameContext>
    {
        public GameContext CreateDbContext(string[] args)
        {
            string configurationPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "game");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(configurationPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<GameContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new System.Exception("DefaultConnection connection string kunne ikke findes i /game/appsettings.json");
            }

            optionsBuilder.UseSqlServer(connectionString);

            return new GameContext(optionsBuilder.Options);
        }
    }
}