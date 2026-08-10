using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;

namespace Domain.StaticData.Data;

public sealed class EdictData : IModifierProvider
{
    public EdictTypeEnum EdictType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BenefitDescription { get; set; } = string.Empty;
    public string DownsideDescription { get; set; } = string.Empty;
    public bool BenefitImplemented { get; set; }
    public bool DownsideImplemented { get; set; }
    public List<Modifier> ModifiersInternal { get; set; } = new();
    public IEnumerable<Modifier> GetModifiers() => ModifiersInternal;
}
