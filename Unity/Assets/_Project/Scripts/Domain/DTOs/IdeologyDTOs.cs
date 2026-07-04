using Assets._Project.Scripts.Domain.Enums;
using Project.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Project.Network.Models;

namespace Project.Scripts.Domain.DTOs
{
    public enum IdeologyFocusEffectKindEnum { Modifier, Instant, Triggered, Conditional }
    public enum IdeologyFocusTargetScopeEnum { City, WorldPlayer, OutgoingDeployment, CombatAtCity }
    public static class IdeologyFocusExtensions
    {
        public static string ToFriendlyName(this IdeologyFocusNameEnum focus)
        {
            // Indsætter et mellemrum foran alle store bogstaver (undtagen det første)
            // Eksempel: "AcceleratedConscription" bliver til "Accelerated Conscription"
            return Regex.Replace(focus.ToString(), "([a-z])([A-Z])", "$1 $2");
        }
    }

    public class IdeologyOverviewDTO
    {
        public string Message { get; set; } = string.Empty;
        public IdeologyDTO IdeologyDTO { get; set; } = new IdeologyDTO();
        public List<IdeologyFocusDTO> IdeologyFocuses { get; set; } = new List<IdeologyFocusDTO>();
    }

    public class IdeologyDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IdeologyTypeEnum IdeologyType { get; set; }
        public List<ModifierDTO> ModifiersInternal { get; set; } = new List<ModifierDTO>();
    }

    public class IdeologyFocusRequestDTO
    {
        public IdeologyFocusNameEnum IdeologyFocusName { get; set; }
        public Guid CityId { get; set; }
    }

    public class IdeologyFocusAnswerDTO
    {
        public IdeologyFocusNameEnum? IdeologyFocusName { get; set; }
        public Guid? CityId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public IdeologyFocusEffectResultDTO EffectResult { get; set; }

        public IdeologyFocusAnswerDTO() { }

        public IdeologyFocusAnswerDTO(IdeologyFocusNameEnum? ideologyFocusName, Guid? cityId, string message, bool success)
        {
            IdeologyFocusName = ideologyFocusName;
            CityId = cityId;
            Message = message;
            Success = success;
        }
    }

    public class IdeologyFocusEffectResultDTO
    {
        public string Summary { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public int GrantedQuantity { get; set; }
        public List<UnitStackDTO> GrantedUnits { get; set; } = new List<UnitStackDTO>();
    }

    public class IdeologyFocusDTO
    {
        public IdeologyFocusNameEnum Name { get; set; }
        public double IdeologyFocusPointCost { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<ModifierDTO> ModifiersInternal { get; set; } = new List<ModifierDTO>();
        public bool AlreadyEnacted { get; set; }
        public TimeSpan? ActiveTime { get; set; }
        public DateTime ExpirationTime { get; set; }
        public IdeologyFocusEffectKindEnum EffectKind { get; set; }
        public IdeologyFocusTargetScopeEnum TargetScope { get; set; }
        public bool CanRepeat { get; set; }
        public bool ConsumesOnTrigger { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string UnavailableReason { get; set; }
    }
}
