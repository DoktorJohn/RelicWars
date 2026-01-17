using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Domain.StaticData.Readers
{
    public class RankingDataReader
    {
        private List<RankingEntryData> _cachedRankings = new List<RankingEntryData>();
        private string _storedPath = string.Empty; // Vi gemmer stien til auto-reload
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly double _cacheMinutes = 1.0;

        /// <summary>
        /// Sætter stien og loader data første gang.
        /// </summary>
        public void Load(string path)
        {
            _storedPath = path;
            ForceReloadFromDisk();
        }

        /// <summary>
        /// Henter data. Hvis cachen er for gammel, forsøger den at læse filen igen.
        /// </summary>
        public List<RankingEntryData> GetGlobalRankings()
        {
            // Auto-refresh logik indbygget her
            if (IsCacheExpired())
            {
                ForceReloadFromDisk();
            }

            return _cachedRankings;
        }

        private void ForceReloadFromDisk()
        {
            if (string.IsNullOrEmpty(_storedPath) || !File.Exists(_storedPath))
            {
                // Hvis filen ikke findes (f.eks. ved første start), returnerer vi bare det vi har (tom liste)
                return;
            }

            try
            {
                // Vi bruger FileStream med FileShare.ReadWrite for at undgå crash 
                // når generatoren skriver til filen samtidig.
                using var fs = new FileStream(_storedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, Encoding.UTF8);

                string json = sr.ReadToEnd();
                var data = JsonSerializer.Deserialize<List<RankingEntryData>>(json);

                if (data != null)
                {
                    _cachedRankings = data;
                    _lastUpdate = DateTime.UtcNow;
                }
            }
            catch
            {
                // Ved fejl (f.eks. fil låst midlertidigt) beholder vi den gamle cache
            }
        }

        private bool IsCacheExpired()
        {
            return (DateTime.UtcNow - _lastUpdate).TotalMinutes >= _cacheMinutes;
        }
    }
}