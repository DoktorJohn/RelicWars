using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.User
{
    public class WorldPlayer : BaseEntity, IModifierProvider
    {
        public double Coins { get; set; }
        public double IdeologyFocusPoints { get; set; }
        public IdeologyTypeEnum Ideology { get; set; }
        public DateTime LastResourceUpdate { get; set; } = DateTime.UtcNow;
        public AllianceRoleEnum AllianceRole { get; set; } = AllianceRoleEnum.None;

        //Navprops
        public List<City> Cities { get; set; } = new();
        public List<Research> CompletedResearches { get; set; } = new();
        public List<UnitDeployment> UnitDeployments { get; set; } = new();
        public List<UnitStack> UnitStacks { get; set; } = new();
        public List<Modifier> ModifiersInternal { get; set; } = new();
        public List<ModifierTagEnum> ModifiersThatAffectsThis { get; set; } = new();
        
        public List<ConversationParticipant> ConversationParticipants { get; set; } = new();
        public DailyObjectiveSet? DailyObjectiveSet { get; set; }

        //Foreign keys
        public Guid? AllianceId { get; set; }
        public Alliance? Alliance { get; set; }
        public Guid PlayerProfileId { get; set; }
        public PlayerProfile PlayerProfile { get; set; } = null!;
        public Guid WorldId { get; set; }
        public World World { get; set; } = null!;

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }

    }
}
