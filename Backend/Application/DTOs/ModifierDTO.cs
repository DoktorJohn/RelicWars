using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class ModifierDTO
    {
        public double Value { get; set; }
        public ModifierTypeEnum ModifierType { get; set; }
        public ModifierTagEnum ModifierTag { get; set; }
    }
}
