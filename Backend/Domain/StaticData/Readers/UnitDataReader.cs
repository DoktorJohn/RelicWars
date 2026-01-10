using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.StaticData.Readers
{
    public class UnitDataReader
    {
        private Dictionary<UnitTypeEnum, UnitData> _units = new();

        public void Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Filen {path} blev ikke fundet!");

            string json = File.ReadAllText(path);

            // Vi skal bruge denne converter for at forstå "Militia" i stedet for 0
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var list = JsonSerializer.Deserialize<List<UnitData>>(json, options) ?? new();
            _units = list.ToDictionary(u => u.Type);
        }

        public UnitData GetUnit(UnitTypeEnum type)
        {
            if (_units.TryGetValue(type, out var unit)) return unit;
            throw new Exception($"Enheden {type} blev ikke fundet i data!");
        }
    }
}
