using Application.Interfaces.IServices;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.User;

namespace Application.Services;

public sealed class UnitUnlockCatalog : IUnitUnlockCatalog
{
    private readonly IReadOnlyDictionary<UnitTypeEnum, ResearchData> _unitUnlocks;
    private readonly string _subjugationResearchId;

    public UnitUnlockCatalog(UnitDataReader unitDataReader, ResearchDataReader researchDataReader)
    {
        var unitDefinitions = unitDataReader.GetAll();
        var researchDefinitions = researchDataReader.GetAll();

        var recruitmentEffects = researchDefinitions
            .SelectMany(node => node.Effects
                .Where(effect => effect.Type == ResearchEffectType.UnitRecruitment)
                .Select(effect => new { Node = node, Effect = effect }))
            .ToList();

        var invalidRecruitmentEffects = recruitmentEffects
            .Where(entry => entry.Effect.UnitType is null || entry.Effect.UnitType == UnitTypeEnum.None)
            .Select(entry => entry.Node.Id)
            .ToList();
        if (invalidRecruitmentEffects.Count > 0)
        {
            throw new InvalidOperationException($"Unit recruitment effects require a valid unit type: {string.Join(", ", invalidRecruitmentEffects)}.");
        }

        var duplicateMappings = recruitmentEffects
            .GroupBy(entry => entry.Effect.UnitType!.Value)
            .Where(group => group.Count() != 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicateMappings.Count > 0)
        {
            throw new InvalidOperationException($"Units must have exactly one unlock research: {string.Join(", ", duplicateMappings)}.");
        }

        _unitUnlocks = recruitmentEffects.ToDictionary(entry => entry.Effect.UnitType!.Value, entry => entry.Node);

        var advancedUnits = unitDefinitions.Where(unit => !unit.IsDefaultUnlocked).Select(unit => unit.Type).ToHashSet();
        var missingMappings = advancedUnits.Where(unitType => !_unitUnlocks.ContainsKey(unitType)).ToList();
        var unexpectedMappings = _unitUnlocks.Keys.Where(unitType => !advancedUnits.Contains(unitType)).ToList();
        if (missingMappings.Count > 0 || unexpectedMappings.Count > 0)
        {
            throw new InvalidOperationException(
                $"Unlock catalog mismatch. Missing: {string.Join(", ", missingMappings)}. Unexpected: {string.Join(", ", unexpectedMappings)}.");
        }

        var subjugationNodes = researchDefinitions
            .Where(node => node.Effects.Count(effect => effect.Type == ResearchEffectType.Subjugation) > 0)
            .ToList();
        if (subjugationNodes.Count != 1 ||
            subjugationNodes[0].Effects.Count(effect => effect.Type == ResearchEffectType.Subjugation) != 1)
        {
            throw new InvalidOperationException("The unlock catalog requires exactly one typed subjugation effect.");
        }

        _subjugationResearchId = subjugationNodes[0].Id;
    }

    public ResearchData? GetUnitUnlock(UnitTypeEnum unitType) =>
        _unitUnlocks.TryGetValue(unitType, out var research) ? research : null;

    public bool HasSubjugationUnlock(WorldPlayer worldPlayer) =>
        worldPlayer.CompletedResearches.Any(research => research.ResearchId == _subjugationResearchId);
}
