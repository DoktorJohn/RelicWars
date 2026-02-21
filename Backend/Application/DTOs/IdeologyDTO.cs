using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class IdeologyOverviewDTO
    {
        public string Message { get; set; } = string.Empty;
        public IdeologyDTO IdeologyDTO { get; set; } = new();
        public List<IdeologyFocusDTO> IdeologyFocuses { get; set; } = new();
    }


    public class IdeologyDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IdeologyTypeEnum IdeologyType { get; set; }
        public List<ModifierDTO> ModifiersInternal { get; set; } = new();
    }
}

