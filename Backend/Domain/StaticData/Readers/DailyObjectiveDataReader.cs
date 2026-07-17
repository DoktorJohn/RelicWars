using Domain.Enums;
using Domain.StaticData.Data;
using Newtonsoft.Json;

namespace Domain.StaticData.Readers
{
    public sealed class DailyObjectiveDataReader
    {
        private DailyObjectiveCatalogData _catalog = new();

        public DailyObjectiveCatalogData Catalog => _catalog;

        public void Load(string path)
        {
            var json = File.ReadAllText(path);
            _catalog = JsonConvert.DeserializeObject<DailyObjectiveCatalogData>(json)
                ?? throw new InvalidOperationException("Daily objective catalog is empty.");
            Validate(_catalog);
        }

        public DailyObjectiveDefinitionData GetDefinition(int id) =>
            _catalog.Definitions.FirstOrDefault(definition => definition.Id == id)
            ?? throw new KeyNotFoundException($"Daily objective definition {id} was not found.");

        private static void Validate(DailyObjectiveCatalogData catalog)
        {
            if (catalog.Definitions.Count != 51 || catalog.Definitions.Select(x => x.Id).Distinct().Count() != 51)
                throw new InvalidOperationException("Daily objective catalog must contain 51 unique definition ids.");
            if (catalog.Definitions.Any(x => x.Id <= 0 || x.Level != x.Id || string.IsNullOrWhiteSpace(x.Name) ||
                                             string.IsNullOrWhiteSpace(x.CompletionInfo) || x.Target <= 0 ||
                                             !Enum.IsDefined(x.Tier) || !Enum.IsDefined(x.ProgressType)))
                throw new InvalidOperationException("Daily objective catalog contains an invalid definition.");
            if (catalog.Selection.FixedSlots != 10 || catalog.Selection.WeightedSlots != 10)
                throw new InvalidOperationException("Daily objective selection must define 10 fixed and 10 weighted slots.");
            if (catalog.Definitions.Count(x => x.Tier == DailyObjectiveTierEnum.Fixed) < catalog.Selection.FixedSlots)
                throw new InvalidOperationException("Daily objective fixed pool is too small.");
            if (catalog.Selection.Weights.GetValueOrDefault(DailyObjectiveTierEnum.Uncommon) != 65 ||
                catalog.Selection.Weights.GetValueOrDefault(DailyObjectiveTierEnum.Rare) != 30 ||
                catalog.Selection.Weights.GetValueOrDefault(DailyObjectiveTierEnum.Unique) != 5)
                throw new InvalidOperationException("Daily objective weights must be Uncommon 65, Rare 30 and Unique 5.");
            if (catalog.Definitions.Count(x => x.Tier != DailyObjectiveTierEnum.Fixed) < catalog.Selection.WeightedSlots)
                throw new InvalidOperationException("Daily objective weighted pool is too small.");
        }
    }
}
