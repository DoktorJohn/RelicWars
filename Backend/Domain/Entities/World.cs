using Domain.Abstraction;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class World : BaseEntity, IModifierProvider
    {
        public const int TileSize = 10;
        public string Name { get; set; } = string.Empty;
        public string Abbrevation { get; set; } = string.Empty;
        public int XAxis { get; set; }
        public int YAxis { get; set; }
        public int PlayerCount { get; set; }

        [NotMapped]
        public Tile[,] Tiles { get; set; }
        public List<Modifier> ModifiersInternal { get; set; } = new();

        protected World() { InitializeTiles(); } // EF Core bruger denne

        public World(string name, string abbrevation, int xAxis, int yAxis)
        {
            Name = name;
            Abbrevation = abbrevation;
            XAxis = xAxis;
            YAxis = yAxis;
            InitializeTiles();
        }

        private void InitializeTiles()
        {
            Tiles = new Tile[XAxis, YAxis];
            var rnd = new Random();
            // Simpel generator: Fyld verden med tilfældige biomer
            for (int x = 0; x < XAxis; x++)
            {
                for (int y = 0; y < YAxis; y++)
                {
                    Tiles[x, y] = new Tile
                    {
                        TileBiome = (BiomeEnum)rnd.Next(0, 4), // Antager du har en BiomeEnum
                        Citycount = 0
                    };
                }
            }
        }

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
