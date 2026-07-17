using Domain.Abstraction;

namespace Domain.Entities
{
    public class DailyObjectiveAssignment : BaseEntity
    {
        public Guid DailyObjectiveSetId { get; set; }
        public int DefinitionId { get; set; }
        public int Slot { get; set; }
        public double Target { get; set; }
        public double Progress { get; set; }
        public bool IsComplete { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public DailyObjectiveSet DailyObjectiveSet { get; set; } = null!;
    }
}
