using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Services;
using Domain.Entities;
using Domain.User;

namespace Application.Tests;

public class MessagingServiceTests
{
    private static readonly Guid WorldId = Guid.NewGuid();

    [Fact]
    public async Task StartConversationCreatesGroupAndTrimsInput()
    {
        var sender = Player("sender");
        var first = Player("first");
        var second = Player("second");
        var messages = new MemoryMessagingRepository();
        var service = Service(messages, sender, first, second);

        var result = await service.StartConversationAsync(
            sender.Id, new[] { first.Id, second.Id, first.Id, sender.Id }, "  Subject  ", "  Hello  ");

        var conversation = Assert.Single(messages.Conversations);
        Assert.Equal("Subject", conversation.Subject);
        Assert.Equal("Hello", Assert.Single(conversation.Messages).Content);
        Assert.Equal(3, conversation.Participants.Count);
        Assert.True(result.IsGroupConversation);
        Assert.Equal(2, result.Participants.Count(p => p.WorldPlayerId != sender.Id));
        Assert.NotNull(conversation.Participants.Single(p => p.WorldPlayerId == sender.Id).LastReadAt);
        Assert.All(conversation.Participants.Where(p => p.WorldPlayerId != sender.Id), p => Assert.Null(p.LastReadAt));
    }

    [Fact]
    public async Task StartConversationRejectsMissingCrossWorldAndEmptyRecipients()
    {
        var sender = Player("sender");
        var otherWorld = Player("outsider", Guid.NewGuid());
        var service = Service(new(), sender, otherWorld);

        await Assert.ThrowsAsync<ArgumentException>(() => service.StartConversationAsync(sender.Id, [], "s", "body"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.StartConversationAsync(sender.Id, null!, "s", "body"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.StartConversationAsync(sender.Id, [otherWorld.Id], "s", "body"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.StartConversationAsync(sender.Id, [Guid.NewGuid()], "s", "body"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartConversationRejectsEmptyContent(string content)
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Service(new(), sender, receiver).StartConversationAsync(sender.Id, [receiver.Id], "subject", content));
    }

    [Fact]
    public async Task StartConversationRejectsOversizedValues()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var service = Service(new(), sender, receiver);
        await Assert.ThrowsAsync<ArgumentException>(() => service.StartConversationAsync(sender.Id, [receiver.Id], new string('s', 121), "body"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.StartConversationAsync(sender.Id, [receiver.Id], "subject", new string('m', 5001)));
    }

    [Fact]
    public async Task PublicOwnedReportSupportsAttachmentOnlyAndTextWithAttachment()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var report = Report(sender.Id, true);
        var messages = new MemoryMessagingRepository();
        var service = ServiceWithReports(messages, [report], sender, receiver);

        var started = await service.StartConversationAsync(sender.Id, [receiver.Id], "subject", "", report.Id);
        var initial = Assert.Single(messages.Conversations[0].Messages);
        Assert.Equal(report.Id, initial.ReportAttachment?.BattleReportId);
        Assert.Equal(report.Title, started.LastMessageContent);

        var reply = await service.ReplyToConversationAsync(sender.Id, started.Id, "details", report.Id);
        Assert.Equal("details", reply.Content);
        Assert.True(reply.ReportAttachment?.IsAvailable);
        Assert.Equal(report.Title, reply.ReportAttachment?.Report?.Title);
        Assert.Null(reply.ReportAttachment?.Report?.GetType().GetProperty("IsRead"));
    }

    [Fact]
    public async Task AttachmentsRejectPrivateForeignAndMissingReports()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var privateReport = Report(sender.Id, false);
        var foreignReport = Report(receiver.Id, true);
        var service = ServiceWithReports(new(), [privateReport, foreignReport], sender, receiver);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartConversationAsync(sender.Id, [receiver.Id], "s", "body", privateReport.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.StartConversationAsync(sender.Id, [receiver.Id], "s", "body", foreignReport.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.StartConversationAsync(sender.Id, [receiver.Id], "s", "body", Guid.NewGuid()));
    }

    [Fact]
    public async Task PrivateOrDeletedLiveAttachmentBecomesUnavailable()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var report = Report(sender.Id, true);
        var repository = new MemoryMessagingRepository();
        var service = ServiceWithReports(repository, [report], sender, receiver);
        var started = await service.StartConversationAsync(sender.Id, [receiver.Id], "s", "", report.Id);
        var message = repository.Conversations[0].Messages[0];
        repository.Messages.Add(message);

        report.IsPublic = false;
        var privateResult = Assert.Single(await service.GetMessagesAsync(started.Id, receiver.Id, null, 50));
        Assert.False(privateResult.ReportAttachment?.IsAvailable);

        message.ReportAttachment!.BattleReport = null;
        message.ReportAttachment.BattleReportId = null;
        var deletedResult = Assert.Single(await service.GetMessagesAsync(started.Id, receiver.Id, null, 50));
        Assert.False(deletedResult.ReportAttachment?.IsAvailable);
    }

    [Fact]
    public async Task ReplyCreatesIdentityUpdatesConversationAndMarksSenderRead()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var repository = new MemoryMessagingRepository();
        var service = Service(repository, sender, receiver);
        var started = await service.StartConversationAsync(sender.Id, [receiver.Id], "subject", "first");

        var reply = await service.ReplyToConversationAsync(receiver.Id, started.Id, " reply ");

        Assert.NotEqual(Guid.Empty, reply.Id);
        Assert.Equal("reply", reply.Content);
        Assert.True(reply.IsRead);
        Assert.Equal(receiver.Id, reply.SenderId);
        Assert.NotNull(repository.Conversations[0].Participants.Single(p => p.WorldPlayerId == receiver.Id).LastReadAt);
        Assert.Equal(repository.Messages.Single().SentAt, repository.Conversations[0].LastMessageDate);
    }

    [Fact]
    public async Task ReplyAndMessageListingIncludeSenderAllianceId()
    {
        var senderAlliance = new Alliance
        {
            Id = Guid.NewGuid(),
            Name = "Legion",
            Tag = "LEG"
        };
        var receiverAlliance = new Alliance
        {
            Id = Guid.NewGuid(),
            Name = "Cohort",
            Tag = "COH"
        };

        var sender = Player("sender");
        sender.AllianceId = senderAlliance.Id;
        sender.Alliance = senderAlliance;
        senderAlliance.Members = new List<WorldPlayer> { sender };

        var receiver = Player("receiver");
        receiver.AllianceId = receiverAlliance.Id;
        receiver.Alliance = receiverAlliance;
        receiverAlliance.Members = new List<WorldPlayer> { receiver };

        var repository = new MemoryMessagingRepository();
        var service = Service(repository, sender, receiver);
        var started = await service.StartConversationAsync(sender.Id, [receiver.Id], "subject", "first");

        var reply = await service.ReplyToConversationAsync(receiver.Id, started.Id, " reply ");
        Assert.Equal(receiverAlliance.Id, reply.SenderAllianceId);

        var messages = await service.GetMessagesAsync(started.Id, sender.Id, null, 50);
        var replyMessage = Assert.Single(messages.Where(message => message.Content == "reply"));
        Assert.Equal(receiverAlliance.Id, replyMessage.SenderAllianceId);
        Assert.Equal(receiverAlliance.Name, replyMessage.SenderAllianceName);
    }

    [Fact]
    public async Task NonParticipantCannotReadOrReply()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var outsider = Player("outsider");
        var repository = new MemoryMessagingRepository();
        var service = Service(repository, sender, receiver, outsider);
        var started = await service.StartConversationAsync(sender.Id, [receiver.Id], "subject", "first");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ReplyToConversationAsync(outsider.Id, started.Id, "reply"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetMessagesAsync(started.Id, outsider.Id, null, 50));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.MarkConversationAsReadAsync(started.Id, outsider.Id));
    }

    [Fact]
    public async Task GetMessagesClampsPageSizeAndCalculatesReadState()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var repository = new MemoryMessagingRepository();
        var service = Service(repository, sender, receiver);
        var started = await service.StartConversationAsync(sender.Id, [receiver.Id], "subject", "first");
        var conversation = repository.Conversations[0];
        var readAt = DateTime.UtcNow.AddMinutes(-1);
        conversation.Participants.Single(p => p.WorldPlayerId == receiver.Id).LastReadAt = readAt;
        repository.Messages.AddRange(new[]
        {
            Message(conversation.Id, sender, readAt.AddMinutes(-1), "read"),
            Message(conversation.Id, sender, readAt.AddMinutes(1), "unread"),
            Message(conversation.Id, receiver, readAt.AddMinutes(2), "mine")
        });

        var result = await service.GetMessagesAsync(started.Id, receiver.Id, null, 500);

        Assert.Equal(100, repository.LastTake);
        Assert.True(result.Single(m => m.Content == "read").IsRead);
        Assert.False(result.Single(m => m.Content == "unread").IsRead);
        Assert.True(result.Single(m => m.Content == "mine").IsRead);
    }

    [Fact]
    public async Task SearchNormalizesQueryAndSkipsTooShortQueries()
    {
        var player = Player("Alice");
        var players = new MemoryWorldPlayerRepository([player]);
        var service = new MessagingService(new MemoryMessagingRepository(), players, new TestPlayerAccessService([player]), new MemoryBattleReportRepository([]));

        Assert.Empty(await service.SearchPlayersAsync(WorldId, " a "));
        Assert.Null(players.LastSearch);
        var result = await service.SearchPlayersAsync(WorldId, "  Ali  ");
        Assert.Equal("Ali", players.LastSearch);
        Assert.Equal(player.Id, Assert.Single(result).WorldPlayerId);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SearchPlayersAsync(WorldId, new string('x', 51)));
    }

    [Fact]
    public async Task MarkMessageReadOnlyAdvancesRecipientsReadState()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var repository = new MemoryMessagingRepository();
        var service = Service(repository, sender, receiver);
        var started = await service.StartConversationAsync(sender.Id, [receiver.Id], "subject", "first");
        var message = repository.Conversations[0].Messages[0];

        await service.MarkMessageAsReadAsync(message.Id, sender.Id);
        Assert.Equal(0, repository.ParticipantUpdateCount);
        await service.MarkMessageAsReadAsync(message.Id, receiver.Id);
        Assert.Equal(1, repository.ParticipantUpdateCount);
        Assert.NotNull(repository.Conversations[0].Participants.Single(p => p.WorldPlayerId == receiver.Id).LastReadAt);
    }

    [Fact]
    public async Task DeleteConversationOnlyHidesItForRequestingParticipant()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var repository = new MemoryMessagingRepository();
        var service = Service(repository, sender, receiver);
        var started = await service.StartConversationAsync(sender.Id, [receiver.Id], "subject", "first");

        await service.DeleteConversationAsync(started.Id, sender.Id);

        var participants = repository.Conversations[0].Participants;
        Assert.NotNull(participants.Single(p => p.WorldPlayerId == sender.Id).DeletedAt);
        Assert.Null(participants.Single(p => p.WorldPlayerId == receiver.Id).DeletedAt);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetMessagesAsync(started.Id, sender.Id, null, 50));
        var receiverMessages = await service.GetMessagesAsync(started.Id, receiver.Id, null, 50);
        Assert.NotNull(receiverMessages);
    }

    [Fact]
    public async Task DeleteConversationRejectsNonParticipantAndRepeatedDelete()
    {
        var sender = Player("sender");
        var receiver = Player("receiver");
        var outsider = Player("outsider");
        var repository = new MemoryMessagingRepository();
        var service = Service(repository, sender, receiver, outsider);
        var started = await service.StartConversationAsync(sender.Id, [receiver.Id], "subject", "first");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteConversationAsync(started.Id, outsider.Id));
        await service.DeleteConversationAsync(started.Id, sender.Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteConversationAsync(started.Id, sender.Id));
    }

    private static MessagingService Service(MemoryMessagingRepository messages, params WorldPlayer[] players) =>
        new(messages, new MemoryWorldPlayerRepository(players), new TestPlayerAccessService(players), new MemoryBattleReportRepository([]));

    private static MessagingService ServiceWithReports(MemoryMessagingRepository messages, IEnumerable<BattleReport> reports, params WorldPlayer[] players) =>
        new(messages, new MemoryWorldPlayerRepository(players), new TestPlayerAccessService(players), new MemoryBattleReportRepository(reports));

    private static BattleReport Report(Guid ownerId, bool isPublic) => new()
    {
        Id = Guid.NewGuid(), WorldPlayerId = ownerId, IsPublic = isPublic,
        Title = "Battle at the gate", Body = "Full report", OccurredAt = DateTime.UtcNow
    };

    private static WorldPlayer Player(string name, Guid? worldId = null) => new()
    {
        Id = Guid.NewGuid(), WorldId = worldId ?? WorldId,
        PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = name }
    };

    private static Message Message(Guid conversationId, WorldPlayer sender, DateTime sentAt, string content) => new()
    {
        Id = Guid.NewGuid(), ConversationId = conversationId, SenderId = sender.Id,
        Sender = sender, SentAt = sentAt, Content = content
    };

    private sealed class MemoryWorldPlayerRepository(IEnumerable<WorldPlayer> players) : IWorldPlayerRepository
    {
        private readonly List<WorldPlayer> _players = players.ToList();
        public string? LastSearch { get; private set; }
        public Task<WorldPlayer?> GetByIdAsync(Guid id) => Task.FromResult(_players.SingleOrDefault(p => p.Id == id));
        public Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id) => GetByIdAsync(id);
        public Task AddAsync(WorldPlayer user) { _players.Add(user); return Task.CompletedTask; }
        public Task UpdateAsync(WorldPlayer user) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<List<WorldPlayer>>? GetAllAsync() => Task.FromResult(_players.ToList());
        public Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId) => Task.FromResult(_players.SingleOrDefault(p => p.PlayerProfileId == profileId && p.WorldId == worldId));
        public Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId) => Task.FromResult(_players.Where(p => p.AllianceId == allianceId).ToList());
        public Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery)
        {
            LastSearch = usernameQuery;
            return Task.FromResult(_players.Where(p => p.WorldId == worldId && (p.PlayerProfile.UserName?.Contains(usernameQuery, StringComparison.OrdinalIgnoreCase) ?? false)).ToList());
        }
    }

    private sealed class MemoryMessagingRepository : IMessagingRepository
    {
        public List<Conversation> Conversations { get; } = [];
        public List<Message> Messages { get; } = [];
        public int LastTake { get; private set; }
        public int ParticipantUpdateCount { get; private set; }
        public Task<Conversation?> GetConversationByIdAsync(Guid id) => Task.FromResult(Conversations.SingleOrDefault(c => c.Id == id));
        public Task<Conversation?> GetConversationForAccessAsync(Guid id) => GetConversationByIdAsync(id);
        public Task<List<Message>> GetMessagesForConversationAsync(Guid id, DateTime? before, int take)
        {
            LastTake = take;
            return Task.FromResult(Messages.Where(m => m.ConversationId == id && (!before.HasValue || m.SentAt < before)).OrderByDescending(m => m.SentAt).Take(take).OrderBy(m => m.SentAt).ToList());
        }
        public Task<List<ConversationDTO>> GetConversationSummariesForPlayerAsync(Guid id) => Task.FromResult(new List<ConversationDTO>());
        public Task AddConversationAsync(Conversation conversation)
        {
            EnsureMessageSenders(conversation);
            Conversations.Add(conversation);
            return Task.CompletedTask;
        }
        public Task AddMessageAsync(Message message)
        {
            EnsureMessageSender(message);
            Messages.Add(message);
            return Task.CompletedTask;
        }
        public Task UpdateConversationAsync(Conversation conversation) => Task.CompletedTask;
        public Task<ConversationParticipant?> GetConversationParticipantAsync(Guid conversationId, Guid playerId) => Task.FromResult(Conversations.SingleOrDefault(c => c.Id == conversationId)?.Participants.SingleOrDefault(p => p.WorldPlayerId == playerId));
        public Task UpdateConversationParticipantAsync(ConversationParticipant participant) { ParticipantUpdateCount++; return Task.CompletedTask; }
        public Task<Message?> GetMessageAsync(Guid id) => Task.FromResult(Conversations.SelectMany(c => c.Messages).Concat(Messages).SingleOrDefault(m => m.Id == id));
        public Task UpdateMessageAsync(Message message) => Task.CompletedTask;
        public Task<bool> HasUnreadMessagesAsync(Guid id) => Task.FromResult(false);
        public Task<int> CountUnreadMessagesAsync(Guid id) => Task.FromResult(0);

        private static void EnsureMessageSenders(Conversation conversation)
        {
            foreach (var message in conversation.Messages)
            {
                EnsureMessageSender(conversation, message);
            }
        }

        private void EnsureMessageSender(Message message)
        {
            var conversation = Conversations.SingleOrDefault(c => c.Id == message.ConversationId);
            EnsureMessageSender(conversation, message);
        }

        private static void EnsureMessageSender(Conversation? conversation, Message message)
        {
            if (message.Sender != null || conversation == null)
            {
                return;
            }

            var sender = conversation.Participants
                .Select(p => p.WorldPlayer)
                .FirstOrDefault(player => player?.Id == message.SenderId);

            if (sender != null)
            {
                message.Sender = sender;
            }
        }
    }

    private sealed class MemoryBattleReportRepository(IEnumerable<BattleReport> reports) : IBattleReportRepository
    {
        private readonly List<BattleReport> _reports = reports.ToList();
        public Task AddAsync(BattleReport report) { _reports.Add(report); return Task.CompletedTask; }
        public Task<BattleReport?> GetByIdAsync(Guid id) => Task.FromResult(_reports.SingleOrDefault(report => report.Id == id));
        public Task<List<BattleReport>> GetByUserIdAsync(Guid id) => Task.FromResult(_reports.Where(report => report.WorldPlayerId == id).ToList());
        public Task<int> GetUnreadCountAsync(Guid id) => Task.FromResult(0);
        public Task MarkAsReadAsync(Guid id) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) { _reports.RemoveAll(report => report.Id == id); return Task.CompletedTask; }
        public Task SetPublicStatusAsync(Guid id, bool isPublic) { _reports.Single(report => report.Id == id).IsPublic = isPublic; return Task.CompletedTask; }
    }
}
