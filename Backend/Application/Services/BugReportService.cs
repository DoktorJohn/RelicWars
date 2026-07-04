using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;

namespace Application.Services
{
    public class BugReportService : IBugReportService
    {
        public const int MaximumDescriptionLength = 4000;

        private readonly IBugReportRepository _bugReportRepository;
        private readonly ICurrentUserService _currentUserService;

        public BugReportService(IBugReportRepository bugReportRepository, ICurrentUserService currentUserService)
        {
            _bugReportRepository = bugReportRepository;
            _currentUserService = currentUserService;
        }

        public async Task<BugReportDTO> SubmitAsync(string description)
        {
            var normalizedDescription = description?.Trim() ?? string.Empty;
            if (normalizedDescription.Length == 0)
            {
                throw new ArgumentException("Bugbeskrivelsen må ikke være tom.", nameof(description));
            }

            if (normalizedDescription.Length > MaximumDescriptionLength)
            {
                throw new ArgumentException($"Bugbeskrivelsen må højst være {MaximumDescriptionLength} tegn.", nameof(description));
            }

            var bugReport = new BugReport
            {
                Id = Guid.NewGuid(),
                Description = normalizedDescription,
                PlayerProfileId = _currentUserService.GetProfileId()
            };

            await _bugReportRepository.AddAsync(bugReport);

            return new BugReportDTO
            {
                Id = bugReport.Id,
                Description = bugReport.Description,
                CreatedAt = bugReport.DateCreated
            };
        }
    }
}
