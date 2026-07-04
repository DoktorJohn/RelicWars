using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.User
{
    public class PlayerProfile : IdentityUser<Guid>
    {
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<WorldPlayer> WorldPlayers { get; set; } = new();
    }
}
