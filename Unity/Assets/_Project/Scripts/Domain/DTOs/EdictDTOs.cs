using System; using System.Collections.Generic; using Assets._Project.Scripts.Domain.Enums;
namespace Project.Scripts.Domain.DTOs
{
 [Serializable] public class EnactEdictRequestDTO { public EdictTypeEnum EdictType; }
 [Serializable] public class EdictOptionDTO { public EdictTypeEnum EdictType; public string Name; public string BenefitDescription; public string DownsideDescription; public bool BenefitImplemented; public bool DownsideImplemented; public int UsageCount; public int UsageLimit; public bool CanEnact; public EdictAvailabilityReasonEnum AvailabilityReason; }
 [Serializable] public class EdictOverviewDTO { public Guid CityId; public EdictTypeEnum? ActiveEdict; public DateTime? EnactedAtUtc; public DateTime? CooldownEndsAtUtc; public DateTime ServerUtcNow; public List<EdictOptionDTO> Options = new List<EdictOptionDTO>(); }
}
