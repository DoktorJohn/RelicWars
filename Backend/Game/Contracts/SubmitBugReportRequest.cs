using System.ComponentModel.DataAnnotations;

namespace Game.Contracts
{
    public class SubmitBugReportRequest
    {
        [Required]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;
    }
}
