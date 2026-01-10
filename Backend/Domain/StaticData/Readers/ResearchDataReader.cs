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
    public class ResearchDataReader
    {
        private Dictionary<string, ResearchData> _researchNodes = new();

        public void Load(string path)
        {
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var list = JsonSerializer.Deserialize<List<ResearchData>>(json, options) ?? new();
            _researchNodes = list.ToDictionary(r => r.Id);
        }

        public ResearchData GetNode(string id)
        {
            if (_researchNodes.TryGetValue(id, out var node)) return node;
            throw new Exception($"Research node {id} ikke fundet!");
        }

        public List<ResearchData> GetAll() => _researchNodes.Values.ToList();
    }
}
