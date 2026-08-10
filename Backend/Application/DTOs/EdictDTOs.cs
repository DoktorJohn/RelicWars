using Domain.Enums;

namespace Application.DTOs;

public sealed record EnactEdictRequestDTO(EdictTypeEnum EdictType);
public sealed record EdictOptionDTO(
    EdictTypeEnum EdictType, string Name, string BenefitDescription, string DownsideDescription,
    bool BenefitImplemented, bool DownsideImplemented, int UsageCount, int UsageLimit,
    bool CanEnact, EdictAvailabilityReasonEnum AvailabilityReason);
public sealed record EdictOverviewDTO(
    Guid CityId, EdictTypeEnum? ActiveEdict, DateTime? EnactedAtUtc, DateTime? CooldownEndsAtUtc,
    DateTime ServerUtcNow, IReadOnlyList<EdictOptionDTO> Options);

public enum EdictAvailabilityReasonEnum { Available, AlreadyActive, Cooldown, UsageLimitReached }
