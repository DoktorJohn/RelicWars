using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class AllianceService : IAllianceService
    {
        private readonly IAllianceRepository _allianceRepo;
        private readonly IWorldPlayerRepository _playerRepo;
        private readonly IRankingService _rankingService;
        private readonly ILogger<AllianceService> _logger;

        public AllianceService(
            IAllianceRepository allianceRepo,
            IWorldPlayerRepository playerRepo,
            ILogger<AllianceService> logger,
            IRankingService rankingService)
        {
            _allianceRepo = allianceRepo;
            _playerRepo = playerRepo;
            _logger = logger;
            _rankingService = rankingService;
        }

        public async Task<AllianceDTO> GetAllianceInfo(Guid allianceId)
        {
            // Normalt ville man slå op på ID, men her bruger vi dto.Id fra input
            var alliance = await _allianceRepo.GetByIdAsync(allianceId);

            if (alliance == null) throw new Exception("Alliance not found");

            var allRankings = await _rankingService.GetRankings();
            long calculatedTotalPoints = 0;

            if (alliance.Members != null && alliance.Members.Count > 0)
            {
                // Vi finder de ranking entries, hvor WorldPlayerId findes i alliancens medlemsliste
                calculatedTotalPoints = allRankings
                    .Where(r => alliance.Members.Any(m => m.Id == r.WorldPlayerId))
                    .Sum(r => (long)r.TotalPoints);
            }

            // Mapping (kunne gøres med AutoMapper)
            return new AllianceDTO(
                alliance.Id,
                alliance.Name,
                alliance.Tag,
                alliance.Description,
                alliance.BannerImageUrl,
                calculatedTotalPoints,
                alliance.Members.Count, // Antager Members opdateres, ellers brug Members.Count
                alliance.MaxPlayers
            );
        }

        public async Task<AllianceDTO> CreateAlliance(CreateAllianceDTO dto)
        {
            // 1. Hent grundlæggeren
            var founder = await _playerRepo.GetByIdAsync(dto.WorldPlayerIdFounder);
            if (founder == null) throw new Exception("Founder player not found.");

            // 2. Tjek om spilleren allerede er i en alliance
            if (founder.AllianceId.HasValue)
            {
                throw new Exception("Player is already in an alliance.");
            }

            long initialTotalPoints = 0;

            if (founder.Cities != null)
            {
                initialTotalPoints = founder.Cities.Sum(c => c.Points);
            }

            // 3. Tjek om navnet er taget (kræver en metode i repo)
            bool nameExists = await _allianceRepo.NameExistsAsync(dto.Name);
            if (nameExists) throw new Exception("Alliance name is taken.");

            // 4. Opret Alliancen
            var newAlliance = new Alliance
            {
                Name = dto.Name,
                Tag = dto.Tag,
                Description = "New Alliance",
                Members = new List<WorldPlayer> { founder }
            };

            // Gem først alliancen for at få et ID
            await _allianceRepo.AddAsync(newAlliance);

            // 5. Opdater grundlæggeren til Leader
            founder.AllianceId = newAlliance.Id;
            founder.AllianceRole = AllianceRoleEnum.Founder;

            await _playerRepo.UpdateAsync(founder);

            return new AllianceDTO(
                newAlliance.Id,
                newAlliance.Name,
                newAlliance.Tag,
                newAlliance.Description,
                newAlliance.BannerImageUrl,
                initialTotalPoints,
                1,
                newAlliance.MaxPlayers
            );
        }

        public async Task<bool> DisbandAlliance(DisbandAllianceDTO dto)
        {
            var player = await _playerRepo.GetByIdAsync(dto.WorldPlayerId);
            var alliance = await _allianceRepo.GetByIdWithMembersAsync(dto.AllianceId); // Vigtigt at inkludere medlemmer

            if (player == null || alliance == null) return false;

            // Rettighedstjek: Kun lederen kan opløse
            if (player.AllianceId != alliance.Id || player.AllianceRole != AllianceRoleEnum.Founder)
            {
                throw new Exception("Only the Alliance Leader can disband the alliance.");
            }

            // Fjern alliancen fra alle medlemmer
            // Bemærk: Dette kræver at vi har hentet medlemmerne. 
            // Hvis 'Members' kun er Guids, skal vi hente alle spillere med AllianceId == dto.AllianceId
            var members = await _playerRepo.GetAllByAllianceIdAsync(dto.AllianceId);

            foreach (var member in members)
            {
                member.AllianceId = null;
                member.AllianceRole = AllianceRoleEnum.None;
                await _playerRepo.UpdateAsync(member);
            }

            await _allianceRepo.DeleteAsync(alliance);
            return true;
        }

        public async Task<bool> InviteToAlliance(InviteToAllianceDTO dto)
        {
            // I en rigtig app ville dette oprette en "Invitation" record.
            // Her laver vi en "Instant Join" for simpelhedens skyld, hvis det er ønsket,
            // eller bare validerer at invitationen kan sendes.

            var inviter = await _playerRepo.GetByIdAsync(dto.WorldPlayerIdInviter);
            var invited = await _playerRepo.GetByIdAsync(dto.WorldPlayerIdInvited);

            if (inviter == null || invited == null) return false;

            // Tjek rettigheder
            if (inviter.AllianceRole <= AllianceRoleEnum.Leader )
                throw new Exception("You do not have permission to invite.");

            // Tjek om den inviterede er ledig
            if (invited.AllianceId.HasValue)
                throw new Exception("Target player is already in an alliance.");

            // --- SCENARIO A: Direkte tilføjelse (Simpelt) ---
            invited.AllianceId = inviter.AllianceId;
            invited.AllianceRole = AllianceRoleEnum.Member; // Start som recruit
            await _playerRepo.UpdateAsync(invited);

            // Opdater alliance member liste hvis I bruger den manuelt
            var alliance = await _allianceRepo.GetByIdAsync(inviter.AllianceId.Value);
            if (alliance != null)
            {
                alliance.Members.Add(invited);
                await _allianceRepo.UpdateAsync(alliance);
            }

            return true;

            // --- SCENARIO B: Opret Invitation (Mere korrekt) ---
            // await _invitationRepo.AddAsync(new Invitation(From: inviter.AllianceId, To: invited.Id));
            // return true;
        }

        public async Task<bool> KickPlayer(KickPlayerFromAllianceDTO dto)
        {
            var kicker = await _playerRepo.GetByIdAsync(dto.WorldPlayerIdKicker);
            var kicked = await _playerRepo.GetByIdAsync(dto.WorldPlayerIdKicked);

            if (kicker == null || kicked == null) return false;

            // Er de i samme alliance?
            if (kicker.AllianceId != kicked.AllianceId || kicker.AllianceId == null)
                throw new Exception("Players are not in the same alliance.");

            // Rettighedstjek: Man skal være Officer+ for at kicke, og man kan ikke kicke en med højere/samme rank
            if (kicker.AllianceRole <= AllianceRoleEnum.Member)
                throw new Exception("Insufficient permissions.");

            if (kicked.AllianceRole >= kicker.AllianceRole)
                throw new Exception("Cannot kick a member with equal or higher rank.");

            // Udfør Kick
            var allianceId = kicked.AllianceId.Value;

            kicked.AllianceId = null;
            kicked.AllianceRole = AllianceRoleEnum.None;
            await _playerRepo.UpdateAsync(kicked);

            // Opdater Alliance Members listen manuelt hvis nødvendigt
            var alliance = await _allianceRepo.GetByIdAsync(allianceId);
            if (alliance != null && alliance.Members.Contains(kicked))
            {
                alliance.Members.Remove(kicked);
                await _allianceRepo.UpdateAsync(alliance);
            }

            return true;
        }
    }
}