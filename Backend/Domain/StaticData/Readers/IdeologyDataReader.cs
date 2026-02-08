using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.StaticData.Readers
{
    public class IdeologyDataReader
    {
        private Dictionary<IdeologyTypeEnum, IdeologyData> _ideologies = new();

        public void Load(string path)
        {
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var list = JsonSerializer.Deserialize<List<IdeologyData>>(json, options) ?? new();

            _ideologies = list.ToDictionary(r => r.IdeologyType);
        }

        public IdeologyData GetIdeology(IdeologyTypeEnum type)
        {
            if (_ideologies.TryGetValue(type, out var ideology)) return ideology;
            throw new Exception($"Ideology {type} ikke fundet!");
        }

        public List<IdeologyData> GetAll() => _ideologies.Values.ToList();
    }
}
