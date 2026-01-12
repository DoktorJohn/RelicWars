using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record CityDetailsDTO(
     Guid Id,
     string Name,
     int Points,
     double Wood,
     double Stone,
     double Metal,
     int X,
     int Y,
     PopulationDTO Population,
     List<BuildingDTO> Buildings,
     List<UnitStackDTO> UnitStacks,
     List<UnitDeploymentDTO> Deployments
 );

    public record CityMapDTO(
    Guid Id,
    string Name,
    int X,
    int Y,
    int Points,
    string OwnerName,
    string AllianceTag
);

    public class CityControllerGetDetailedCityInformationDTO
    {
        public Guid CityId { get; set; }
        public string CityName { get; set; } = string.Empty;

        // Ressourcebeholdning
        public double CurrentWoodAmount { get; set; }
        public double CurrentStoneAmount { get; set; }
        public double CurrentMetalAmount { get; set; }
        public double CurrentSilverAmount { get; set; }

        public double MaxWoodCapacity { get; set; }
        public double MaxStoneCapacity { get; set; }
        public double MaxMetalCapacity { get; set; }

        public double WoodProductionPerHour { get; set; }
        public double StoneProductionPerHour { get; set; }
        public double MetalProductionPerHour { get; set; }

        public int CurrentPopulationUsage { get; set; }
        public int MaxPopulationCapacity { get; set; }

        // Liste over bygninger med dedikeret DTO til denne specifikke forespørgsel
        public List<CityControllerGetDetailedCityInformationBuildingDTO> BuildingList { get; set; } = new();
    }

    /// <summary>
    /// Bygnings-data specifikt knyttet til CityControllerGetDetailedCityInformation forespørgslen.
    /// </summary>
    public class CityControllerGetDetailedCityInformationBuildingDTO
    {
        public Guid BuildingId { get; set; }
        public BuildingTypeEnum BuildingType { get; set; }
        public int CurrentLevel { get; set; }
        public DateTime? UpgradeStartedAt { get; set; }
        public DateTime? UpgradeFinishedAt { get; set; }
        public bool IsCurrentlyUpgrading { get; set; }
    }
}
