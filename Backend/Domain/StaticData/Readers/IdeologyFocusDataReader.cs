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
    public class IdeologyFocusDataReader
    {
        private Dictionary<string, IdeologyFocusData> _ideologyFocuses = new();

        public void Load(string path)
        {
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var list = JsonSerializer.Deserialize<List<IdeologyFocusData>>(json, options) ?? new();
            _ideologyFocuses = list.ToDictionary(r => r.Name.ToString());
        }

        public IdeologyFocusData GetIdeology(string name)
        {
            if (_ideologyFocuses.TryGetValue(name, out var ideologyFocus)) return ideologyFocus;
            throw new Exception($"Ideology {name} ikke fundet!");
        }

        public List<IdeologyFocusData> GetAll() => _ideologyFocuses.Values.ToList();
    }
}
