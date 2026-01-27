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
        public string Name { get; set; } = string.Empty;
        public string Abbrevation { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public int MapSeed { get; set; }
        public int PlayerCount { get; set; }

        // Navprop
        public ICollection<WorldMapObject> MapObjects { get; set; } = new List<WorldMapObject>();
        public List<Modifier> ModifiersInternal { get; set; } = new();


        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
