using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.StaticData.Data
{
    public class ModifierData
    {
        public ModifierTagEnum Tag { get; set; }
        public ModifierTypeEnum Type { get; set; }
        public double Value { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
