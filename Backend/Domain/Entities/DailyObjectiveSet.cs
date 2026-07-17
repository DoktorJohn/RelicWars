using Domain.Abstraction;
using Domain.User;

namespace Domain.Entities
{
    public class DailyObjectiveSet : BaseEntity
    {
        public Guid WorldPlayerId { get; set; }
        public DateTime DayStartUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public WorldPlayer WorldPlayer { get; set; } = null!;
        public List<DailyObjectiveAssignment> Assignments { get; set; } = new();
    }
}
