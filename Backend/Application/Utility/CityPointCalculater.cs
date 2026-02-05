using Domain.Entities;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utility
{
    public class CityPointCalculator
    {
        private readonly BuildingDataReader _buildingDataReader;

        public CityPointCalculator(BuildingDataReader buildingDataReader)
        {
            _buildingDataReader = buildingDataReader;
        }

        /// <summary>
        /// Beregner det samlede antal points for en by ved at summere pointværdien 
        /// fra de statiske data for hver bygning baseret på dens nuværende niveau.
        /// </summary>
        public int CalculateTotalPointsForCity(City city)
        {
            int accumulatedPoints = 0;

            foreach (var building in city.Buildings)
            {
                try
                {
                    // Da din Reader returnerer den specifikke konfiguration for et niveau,
                    // henter vi BuildingLevelData direkte for bygningens type og nuværende level.
                    var levelConfig = _buildingDataReader.GetConfig<BuildingLevelData>(building.Type, building.Level);

                    if (levelConfig != null)
                    {
                        accumulatedPoints += levelConfig.Points;
                    }
                }
                catch (Exception)
                {
                    // Hvis et specifikt level ikke findes i data (f.eks. level 0 før konstruktion), 
                    // ignorerer vi det blot for pointberegningen.
                    continue;
                }
            }

            return accumulatedPoints;
        }
    }
}
