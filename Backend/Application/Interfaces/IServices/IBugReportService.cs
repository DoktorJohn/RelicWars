using Application.DTOs;

namespace Application.Interfaces.IServices
{
    public interface IBugReportService
    {
        Task<BugReportDTO> SubmitAsync(string description);
    }
}
