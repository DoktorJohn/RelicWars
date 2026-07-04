using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record IdeologyFocusRequestDTO(IdeologyFocusNameEnum IdeologyFocusName, Guid CityId);
    public record IdeologyFocusEffectResultDTO(
        string Summary,
        int RequestedQuantity,
        int GrantedQuantity,
        List<UnitStackDTO> GrantedUnits);

    public record IdeologyFocusAnswerDTO(
        IdeologyFocusNameEnum? IdeologyFocusName,
        Guid? CityId,
        string Message,
        bool Success,
        IdeologyFocusEffectResultDTO? EffectResult = null);

    public class IdeologyFocusDTO
    {
        public IdeologyFocusNameEnum Name { get; set; }
        public double IdeologyFocusPointCost { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<ModifierDTO> ModifiersInternal { get; set; } = new();
        public bool AlreadyEnacted { get; set; }
        public TimeSpan? ActiveTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public IdeologyFocusEffectKindEnum EffectKind { get; set; }
        public IdeologyFocusTargetScopeEnum TargetScope { get; set; }
        public bool CanRepeat { get; set; }
        public bool ConsumesOnTrigger { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string UnavailableReason { get; set; } = string.Empty;
    }
}
