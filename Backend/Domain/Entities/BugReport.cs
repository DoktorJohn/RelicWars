using Domain.Abstraction;
using Domain.User;

namespace Domain.Entities
{
    public class BugReport : BaseEntity
    {
        public string Description { get; set; } = string.Empty;
        public Guid PlayerProfileId { get; set; }
        public PlayerProfile PlayerProfile { get; set; } = null!;
    }
}
