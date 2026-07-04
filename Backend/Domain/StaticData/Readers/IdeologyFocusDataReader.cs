using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.StaticData.Readers
{
    public class IdeologyFocusDataReader
    {
        private Dictionary<IdeologyFocusNameEnum, IdeologyFocusData> _ideologyFocuses = new();

        public void Load(string path)
        {
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var list = JsonSerializer.Deserialize<List<IdeologyFocusData>>(json, options) ?? new();
            foreach (var focus in list) ConfigureAndValidate(focus);
            _ideologyFocuses = list.ToDictionary(r => r.Name);
        }

        private static void ConfigureAndValidate(IdeologyFocusData focus)
        {
            if (focus.Name is IdeologyFocusNameEnum.LordsLevy or IdeologyFocusNameEnum.NewRecruits)
            {
                focus.EffectKind = IdeologyFocusEffectKindEnum.Instant;
                focus.TargetScope = IdeologyFocusTargetScopeEnum.City;
                focus.CanRepeat = focus.Name == IdeologyFocusNameEnum.LordsLevy;
            }
            else if (focus.Name == IdeologyFocusNameEnum.RoyalMedics)
            {
                focus.EffectKind = IdeologyFocusEffectKindEnum.Triggered;
                focus.TargetScope = IdeologyFocusTargetScopeEnum.CombatAtCity;
                focus.ConsumesOnTrigger = true;
            }
            else if (focus.Name == IdeologyFocusNameEnum.PrivateSecurity)
            {
                focus.EffectKind = IdeologyFocusEffectKindEnum.Conditional;
                focus.TargetScope = IdeologyFocusTargetScopeEnum.OutgoingDeployment;
            }

            if (focus.EffectKind == IdeologyFocusEffectKindEnum.Modifier &&
                (focus.TimeActive == null || focus.ModifiersInternal.Count == 0))
                throw new InvalidDataException($"Modifier focus {focus.Name} requires duration and modifiers.");

            if (focus.EffectKind == IdeologyFocusEffectKindEnum.Instant && focus.TimeActive != null)
                throw new InvalidDataException($"Instant focus {focus.Name} cannot have a duration.");

            if (focus.EffectKind == IdeologyFocusEffectKindEnum.Triggered && focus.TimeActive == null)
                throw new InvalidDataException($"Triggered focus {focus.Name} requires a trigger window.");
        }

        public IdeologyFocusData GetIdeology(IdeologyFocusNameEnum name)
        {
            if (_ideologyFocuses.TryGetValue(name, out var ideologyFocus)) return ideologyFocus;
            throw new Exception($"Ideology {name} ikke fundet!");
        }

        public List<IdeologyFocusData> GetAll() => _ideologyFocuses.Values.ToList();
    }
}
