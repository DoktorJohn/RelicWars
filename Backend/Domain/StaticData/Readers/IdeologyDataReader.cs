using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Domain.StaticData.Readers
{
    public class IdeologyDataReader
    {
        private Dictionary<string, IdeologyData> _ideologies = new();

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
            _ideologies = list.ToDictionary(r => r.Name);
        }

        public IdeologyData GetIdeology(string name)
        {
            if (_ideologies.TryGetValue(name, out var ideology)) return ideology;
            throw new Exception($"Ideology {name} ikke fundet!");
        }

        public List<IdeologyData> GetAll() => _ideologies.Values.ToList();
    }
}
