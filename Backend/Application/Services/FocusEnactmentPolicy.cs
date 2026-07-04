using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;

namespace Application.Services
{
    public class FocusEnactmentPolicy
    {
        public bool CanEnact(IdeologyFocusData data, IEnumerable<IdeologyFocus> records, DateTime now)
        {
            if (data.EffectKind == IdeologyFocusEffectKindEnum.Instant && data.CanRepeat)
                return true;

            var matching = records.Where(x => x.Name == data.Name).ToList();
            if (!data.CanRepeat && matching.Count > 0) return false;
            return !matching.Any(x => x.TimeOfIdeologyStarted <= now &&
                (!x.TimeOfIdeologyFinished.HasValue || x.TimeOfIdeologyFinished > now));
        }

        public bool ShouldPersist(IdeologyFocusData data) =>
            data.EffectKind != IdeologyFocusEffectKindEnum.Instant &&
            (data.TimeActive.HasValue || !data.CanRepeat);
    }
}
