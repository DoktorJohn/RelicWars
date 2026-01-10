using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Application.Services.ResourceService;

namespace Application.Services
{
    public record ResourceSnapshot(double Wood, double Stone, double Metal, DateTime Timestamp);

    public class ResourceService : IResourceService
    {
        private readonly BuildingDataReader _buildingData;
        private readonly ICityStatService _statService;

        public ResourceService(BuildingDataReader buildingData, ICityStatService statService)
        {
            _buildingData = buildingData;
            _statService = statService;
        }

        // Vi tilføjer List<ModifierData> her, så vi kan regne research med
        public ResourceSnapshot CalculateCurrent(City city, DateTime now, List<ModifierData>? accountModifiers = null)
        {
            accountModifiers ??= new List<ModifierData>();

            // Beregn tid i timer
            double hoursPassed = (now - city.LastResourceUpdate).TotalHours;
            if (hoursPassed < 0) hoursPassed = 0;

            // Hent produktion pr. time inkl. modifiers
            double woodProd = GetProduction(city, BuildingTypeEnum.TimberCamp, accountModifiers);
            double stoneProd = GetProduction(city, BuildingTypeEnum.StoneQuarry, accountModifiers); // Rettet fra StoneQuarry
            double metalProd = GetProduction(city, BuildingTypeEnum.MetalMine, accountModifiers); // Rettet fra MetalMine

            double capacity = _statService.GetWarehouseCapacity(city);

            double newWood = Math.Min(capacity, city.Wood + (woodProd * hoursPassed));
            double newStone = Math.Min(capacity, city.Stone + (stoneProd * hoursPassed));
            double newMetal = Math.Min(capacity, city.Metal + (metalProd * hoursPassed));

            return new ResourceSnapshot(newWood, newStone, newMetal, now);
        }

        private double GetProduction(City city, BuildingTypeEnum type, List<ModifierData> accountModifiers)
        {
            var building = city.Buildings.FirstOrDefault(b => b.Type == type);
            if (building == null || building.Level == 0) return 10.0; // Basisproduktion

            // Hent basisdata fra JSON
            var config = _buildingData.GetConfig<BuildingLevelData>(type, building.Level);

            // Vi bruger en formel eller en property fra din JSON som base. 
            // Her antager jeg, at 'Population' feltet i din JSON bruges som produktions-base (f.eks. 150)
            double baseProduction = config.PopulationCost * 20;

            // Definer hvilke tags denne produktion reagerer på
            var tags = new List<ModifierTagEnum> { ModifierTagEnum.ResourceProduction };
            if (type == BuildingTypeEnum.TimberCamp) tags.Add(ModifierTagEnum.Wood);
            if (type == BuildingTypeEnum.StoneQuarry) tags.Add(ModifierTagEnum.Stone);
            if (type == BuildingTypeEnum.MetalMine) tags.Add(ModifierTagEnum.Metal);

            // Kombiner bygningens interne modifiers med spillerens research
            var allRelevantModifiers = config.ModifiersInternal.Concat(accountModifiers);

            // Brug StatCalculator til at finde den endelige produktion pr. time
            return StatCalculator.ApplyModifiers(baseProduction, tags, allRelevantModifiers);
        }
    }
}

