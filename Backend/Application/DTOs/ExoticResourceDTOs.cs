using Domain.Enums;

namespace Application.DTOs
{
    public record ExoticResourceInvestmentRequestDTO(
        int SlotIndex,
        double WoodAmount,
        double StoneAmount,
        double MetalAmount,
        double CoinAmount);

    public record ExoticResourceInvestmentResponseDTO(
        Guid CityId,
        Guid IslandId,
        int SlotIndex,
        int NewTier,
        List<WorldIslandExoticResourceDTO> IslandExoticResources,
        List<CityExoticResourceDTO> CityExoticResources);
}
