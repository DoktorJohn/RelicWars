using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Services
{
    public class BattleReportService : IBattleReportService
    {
        private readonly IBattleReportRepository _battleReportRepository;
        private readonly IPlayerAccessService _playerAccessService;

        public BattleReportService(
            IBattleReportRepository battleReportRepository,
            IPlayerAccessService playerAccessService)
        {
            _battleReportRepository = battleReportRepository;
            _playerAccessService = playerAccessService;
        }

        public async Task<List<BattleReportDTO>> GetBattleReportsAsync(Guid worldPlayerId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);

            var reports = await _battleReportRepository.GetByUserIdAsync(worldPlayerId);
            return reports.Select(MapReport).ToList();
        }

        public async Task<BattleReportUnreadStatusDTO> GetUnreadStatusAsync(Guid worldPlayerId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);

            var unreadCount = await _battleReportRepository.GetUnreadCountAsync(worldPlayerId);
            return new BattleReportUnreadStatusDTO { UnreadCount = unreadCount };
        }

        public async Task MarkBattleReportAsReadAsync(Guid worldPlayerId, Guid battleReportId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);

            var report = await _battleReportRepository.GetByIdAsync(battleReportId);
            if (report == null)
            {
                throw new KeyNotFoundException($"Battle report med ID {battleReportId} blev ikke fundet.");
            }

            if (report.WorldPlayerId != worldPlayerId)
            {
                throw new UnauthorizedAccessException("Battle report tilhører ikke den autentificerede profil.");
            }

            if (!report.IsRead)
            {
                await _battleReportRepository.MarkAsReadAsync(battleReportId);
            }
        }

        public async Task DeleteBattleReportAsync(Guid worldPlayerId, Guid battleReportId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);

            var report = await _battleReportRepository.GetByIdAsync(battleReportId);
            if (report == null)
            {
                throw new KeyNotFoundException($"Battle report med ID {battleReportId} blev ikke fundet.");
            }

            if (report.WorldPlayerId != worldPlayerId)
            {
                throw new UnauthorizedAccessException("Battle report tilhører ikke den autentificerede profil.");
            }

            await _battleReportRepository.DeleteAsync(battleReportId);
        }

        private static BattleReportDTO MapReport(BattleReport report)
        {
            return new BattleReportDTO
            {
                Id = report.Id,
                ReportType = report.ReportType,
                Title = report.Title,
                Body = report.Body,
                OccurredAt = report.OccurredAt,
                IsRead = report.IsRead,
                AttackerLosses = ParseUnitStacks(report.AttackerLossesJson),
                DefenderLosses = ParseUnitStacks(report.DefenderLossesJson),
                RevivedUnits = ParseUnitStacks(report.RevivedUnitsJson),
                AppliedModifiers = ParseStringList(report.AppliedModifiersJson)
            };
        }

        private static List<UnitStackDTO> ParseUnitStacks(string json) =>
            ParseJson<List<UnitStackDTO>>(json) ?? new List<UnitStackDTO>();

        private static List<string> ParseStringList(string json) =>
            ParseJson<List<string>>(json) ?? new List<string>();

        private static T? ParseJson<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (JsonException)
            {
                return default;
            }
        }
    }
}
