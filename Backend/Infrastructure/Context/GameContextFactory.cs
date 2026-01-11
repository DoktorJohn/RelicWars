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
            var optionsBuilder = new DbContextOptionsBuilder<GameContext>();

            string databasePathForDesignDiagnostics = Path.Combine(Directory.GetCurrentDirectory(), "..", "game", "RelicWars_LocalDatabase.db");

            optionsBuilder.UseSqlite($"Data Source={databasePathForDesignDiagnostics}");

            return new GameContext(optionsBuilder.Options);
        }
    }
}