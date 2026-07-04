using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;

namespace Application.Tests
{
    public class BugReportServiceTests
    {
        [Fact]
        public async Task SubmitAsync_PersistsTrimmedDescriptionForAuthenticatedProfile()
        {
            var profileId = Guid.NewGuid();
            var repository = new RecordingBugReportRepository();
            var service = new BugReportService(repository, new StubCurrentUserService(profileId));

            var result = await service.SubmitAsync("  Kortet fryser ved zoom.  ");

            Assert.NotNull(repository.AddedReport);
            Assert.Equal(profileId, repository.AddedReport.PlayerProfileId);
            Assert.Equal("Kortet fryser ved zoom.", repository.AddedReport.Description);
            Assert.Equal(repository.AddedReport.Id, result.Id);
        }

        [Fact]
        public async Task SubmitAsync_RejectsWhitespaceOnlyDescription()
        {
            var repository = new RecordingBugReportRepository();
            var service = new BugReportService(repository, new StubCurrentUserService(Guid.NewGuid()));

            await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitAsync("   "));

            Assert.Null(repository.AddedReport);
        }

        private sealed class RecordingBugReportRepository : IBugReportRepository
        {
            public BugReport? AddedReport { get; private set; }

            public Task AddAsync(BugReport bugReport)
            {
                AddedReport = bugReport;
                return Task.CompletedTask;
            }
        }

        private sealed class StubCurrentUserService : ICurrentUserService
        {
            private readonly Guid _profileId;

            public StubCurrentUserService(Guid profileId)
            {
                _profileId = profileId;
            }

            public Guid GetProfileId() => _profileId;

            public bool TryGetProfileId(out Guid profileId)
            {
                profileId = _profileId;
                return true;
            }
        }
    }
}
