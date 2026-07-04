namespace Application.DTOs
{
    public class BugReportDTO
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
