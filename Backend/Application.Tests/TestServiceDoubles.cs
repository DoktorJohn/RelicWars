using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Domain.Workers;

namespace Application.Tests;

internal sealed class TestPlayerAccessService(
    IEnumerable<WorldPlayer>? players = null,
    IEnumerable<City>? cities = null) : IPlayerAccessService
{
    private readonly List<WorldPlayer> _players = players?.ToList() ?? [];
    private readonly List<City> _cities = cities?.ToList() ?? [];

    public Guid GetAuthenticatedProfileId() => _players.FirstOrDefault()?.PlayerProfileId ?? Guid.Empty;

    public Task<WorldPlayer> RequireOwnedWorldPlayerAsync(Guid worldPlayerId) =>
        Task.FromResult(_players.SingleOrDefault(player => player.Id == worldPlayerId)
            ?? throw new KeyNotFoundException());

    public Task<WorldPlayer> RequireWorldMembershipAsync(Guid worldId) =>
        Task.FromResult(_players.SingleOrDefault(player => player.WorldId == worldId)
            ?? throw new UnauthorizedAccessException());

    public Task<City> RequireOwnedCityAsync(Guid cityId) =>
        Task.FromResult(_cities.SingleOrDefault(city => city.Id == cityId)
            ?? throw new KeyNotFoundException());

    public Task<City> RequireOwnedCityForTownHallAsync(Guid cityId) =>
        Task.FromResult(_cities.SingleOrDefault(city => city.Id == cityId)
            ?? throw new KeyNotFoundException());

    public Task<UnitDeployment> RequireOwnedUnitDeploymentAsync(Guid unitDeploymentId) =>
        throw new NotSupportedException();
}

internal sealed class ImmediateTransactionManager : ITransactionManager
{
    public Task ExecuteAsync(Func<Task> operation) => operation();
    public Task<T> ExecuteAsync<T>(Func<Task<T>> operation) => operation();
}

internal sealed class TestAllianceRepository(
    IEnumerable<AllianceRelation>? relations = null) : IAllianceRepository
{
    private readonly List<AllianceRelation> _relations = relations?.ToList() ?? [];

    public Task<bool> AreAtWarAsync(Guid allianceIdA, Guid allianceIdB) => Task.FromResult(
        _relations.Any(relation =>
            relation.RelationType == AllianceRelationTypeEnum.War &&
            relation.Status == AllianceRelationStatusEnum.Active &&
            ((relation.AllianceIdA == allianceIdA && relation.AllianceIdB == allianceIdB) ||
             (relation.AllianceIdA == allianceIdB && relation.AllianceIdB == allianceIdA))));

    public Task<Alliance?> GetByIdAsync(Guid id) => throw new NotSupportedException();
    public Task AddAsync(Alliance alliance) => throw new NotSupportedException();
    public Task UpdateAsync(Alliance alliance) => throw new NotSupportedException();
    public Task DeleteAsync(Alliance alliance) => throw new NotSupportedException();
    public Task<bool> NameExistsAsync(Guid worldId, string name) => throw new NotSupportedException();
    public Task<Alliance?> GetByIdWithMembersAsync(Guid id) => throw new NotSupportedException();
    public Task<List<AllianceInvitation>> GetInvitationsForPlayerAsync(Guid worldPlayerId, DateTime now) => throw new NotSupportedException();
    public Task<AllianceInvitation?> GetInvitationByIdAsync(Guid invitationId) => throw new NotSupportedException();
    public Task<bool> PendingInvitationExistsAsync(Guid allianceId, Guid worldPlayerId, DateTime now) => throw new NotSupportedException();
    public Task AddInvitationAsync(AllianceInvitation invitation) => throw new NotSupportedException();
    public Task DeleteInvitationAsync(AllianceInvitation invitation) => throw new NotSupportedException();
    public Task DeleteInvitationsForPlayerAsync(Guid worldPlayerId) => throw new NotSupportedException();
    public Task<List<Alliance>> SearchAsync(Guid worldId, string query, int limit) => throw new NotSupportedException();
    public Task<List<AllianceRelation>> GetRelationsAsync(Guid allianceId) => throw new NotSupportedException();
    public Task<AllianceRelation?> GetRelationByIdAsync(Guid relationId) => throw new NotSupportedException();
    public Task<List<AllianceRelation>> GetOpenRelationsBetweenAsync(Guid allianceIdA, Guid allianceIdB) => throw new NotSupportedException();
    public Task AddRelationAsync(AllianceRelation relation) => throw new NotSupportedException();
    public Task UpdateRelationsAsync(IEnumerable<AllianceRelation> relationsToUpdate) => throw new NotSupportedException();
    public Task DeleteRelationsForAllianceAsync(Guid allianceId) => throw new NotSupportedException();
}
