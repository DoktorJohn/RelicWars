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
            // 1. Guard: Find profilen. 
            // VIGTIGT: Sørg for at dit repository bruger .Include(p => p.WorldPlayers).ThenInclude(wp => wp.Cities)
            // ellers vil listen herunder altid være tom, og fejlen vil fortsætte.
            var activeProfile = await _playerProfileRepository.GetByIdAsync(profileIdentifier);

            if (activeProfile == null)
            {
                return new WorldPlayerJoinResponse(false, "Spillerprofil kunne ikke identificeres.", null);
            }

            // 2. Objektivt tjek: Har denne profil allerede en WorldPlayer-entitet i den valgte verden?
            var existingCharacterInWorld = activeProfile.WorldPlayers
                .FirstOrDefault(relationship => relationship.WorldId == targetWorldIdentifier);

            if (existingCharacterInWorld != null)
            {
                // Spilleren er allerede medlem. Vi finder deres hovedby (Capital).
                // Vi antager her, at den første by i listen er deres startby.
                var primaryCity = existingCharacterInWorld.Cities.FirstOrDefault();

                Guid? existingCityId = primaryCity?.Id;

                return new WorldPlayerJoinResponse(
                    true,
                    "Velkommen tilbage. Henter din eksisterende bydata.",
                    existingCityId
                );
            }

            // 3. Hvis vi når hertil, er spilleren ny i denne verden.
            // Vi påbegynder initialisering af en ny karakter og startby.
            var newlyCreatedWorldCharacter = new WorldPlayer
            {
                PlayerProfileId = profileIdentifier,
                WorldId = targetWorldIdentifier,
                Silver = 1000,
                Cities = new List<City>()
            };

            // Gem karakteren først for at generere et ID (afhængigt af din Unit of Work / DB setup)
            await _worldPlayerRepository.AddAsync(newlyCreatedWorldCharacter);

            // 4. Generering af startby
            var startingCapitalCity = new City
            {
                Name = $"{activeProfile.UserName}'s Capital",
                WorldPlayerId = newlyCreatedWorldCharacter.Id,
                Wood = 500,
                Stone = 500,
                Metal = 500,
                LastResourceUpdate = DateTime.UtcNow, // Vigtigt for din ResourceService!
                Buildings = new List<Building>()
            };

            // 5. Tildeling af startbygninger via hjælper-metoden
            InitializeStartingBuildingsForNewCity(startingCapitalCity);

            // Gem byen i databasen
            await _cityRepository.AddAsync(startingCapitalCity);

            return new WorldPlayerJoinResponse(
                true,
                "Ny karakter oprettet og startby er succesfuldt tildelt.",
                startingCapitalCity.Id
            );
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

            targetCity.Buildings.Add(new Building
            {
                Type = BuildingTypeEnum.TimberCamp,
                Level = 1
            });

            targetCity.Buildings.Add(new Building
            {
                Type = BuildingTypeEnum.StoneQuarry,
                Level = 1
            });

            targetCity.Buildings.Add(new Building
            {
                Type = BuildingTypeEnum.MetalMine,
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