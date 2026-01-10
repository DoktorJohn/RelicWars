using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.User;

namespace Application.Services
{
    public class GameWorldService : IGameWorldService
    {
        private readonly IWorldRepository _worldRepository;
        private readonly IWorldPlayerRepository _worldPlayerRepository;
        private readonly ICityRepository _cityRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;

        public GameWorldService(
            IWorldRepository worldRepository,
            IWorldPlayerRepository worldPlayerRepository,
            ICityRepository cityRepository,
            IPlayerProfileRepository playerProfileRepository)
        {
            _worldRepository = worldRepository;
            _worldPlayerRepository = worldPlayerRepository;
            _cityRepository = cityRepository;
            _playerProfileRepository = playerProfileRepository;
        }

        public async Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid profileIdentifier, Guid targetWorldIdentifier)
        {
            // 1. Guard: Find profilen for at sikre, at spilleren eksisterer
            var activeProfile = await _playerProfileRepository.GetByIdAsync(profileIdentifier);
            if (activeProfile == null)
            {
                return new WorldPlayerJoinResponse(false, "Spillerprofil kunne ikke identificeres.", null);
            }

            // 2. Tjek om spilleren allerede eksisterer i den specifikke verden
            var existingCharacterInWorld = activeProfile.WorldPlayers
                .FirstOrDefault(profileInWorld => profileInWorld.WorldId == targetWorldIdentifier);

            if (existingCharacterInWorld != null)
            {
                Guid? lastActiveCity = existingCharacterInWorld.Cities.FirstOrDefault()?.Id;
                return new WorldPlayerJoinResponse(true, "Velkommen tilbage.", lastActiveCity);
            }

            // 3. Initialisering af ny karakter (WorldPlayer) i den valgte verden
            var newWorldCharacter = new WorldPlayer
            {
                PlayerProfileId = profileIdentifier,
                WorldId = targetWorldIdentifier,
                Silver = 1000
            };

            await _worldPlayerRepository.AddAsync(newWorldCharacter);

            // 4. Automatisk generering af startby med start-ressourcer
            var startingCapitalCity = new City
            {
                Name = $"{activeProfile.UserName}'s Capital",
                WorldPlayerId = newWorldCharacter.Id,
                Wood = 500,
                Stone = 500,
                Metal = 500,
                Buildings = new List<Building>() // Initialiser listen
            };

            // 5. Tildeling af startbygninger (Senate, Warehouse, Housing i Level 1)
            InitializeStartingBuildingsForNewCity(startingCapitalCity);

            // Gem byen inklusiv de tilknyttede bygninger
            await _cityRepository.AddAsync(startingCapitalCity);

            return new WorldPlayerJoinResponse(true, "Karakter oprettet og by tildelt med startbygninger.", startingCapitalCity.Id);
        }

        private void InitializeStartingBuildingsForNewCity(City targetCity)
        {
            // Senate Level 1
            targetCity.Buildings.Add(new Building
            {
                Type = BuildingTypeEnum.Senate,
                Level = 1,
            });

            // Warehouse Level 1
            targetCity.Buildings.Add(new Building
            {
                Type = BuildingTypeEnum.Warehouse,
                Level = 1
            });

            // Housing Level 1
            targetCity.Buildings.Add(new Building
            {
                Type = BuildingTypeEnum.Housing,
                Level = 1
            });
        }

        public async Task<List<GameWorldAvailableResponseDTO>> ObtainAllActiveGameWorldsAsync()
        {
            var activeWorlds = await _worldRepository.GetAllAsync();
            return activeWorlds.Select(world => new GameWorldAvailableResponseDTO(
                world.Id, world.Name, world.PlayerCount, 1000, true)).ToList();
        }
    }
}