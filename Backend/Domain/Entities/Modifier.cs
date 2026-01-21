using Domain.Enums;

namespace Domain.Entities
{
    public class Modifier
    {
        public ModifierTagEnum Tag { get; set; }
        public ModifierTypeEnum Type { get; set; }
        public double Value { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
