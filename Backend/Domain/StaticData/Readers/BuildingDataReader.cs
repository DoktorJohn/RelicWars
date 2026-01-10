using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Domain.StaticData.Readers
{
    public class BuildingDataReader
    {
        private Dictionary<BuildingTypeEnum, Dictionary<int, JsonElement>> _rawData = new();

        public void Load(string path)
        {
            string json = File.ReadAllText(path);

            // 1. Vi deserialiserer først til en Dictionary med Lister (da det er sådan filen er bygget)
            var tempMap = JsonSerializer.Deserialize<Dictionary<BuildingTypeEnum, List<JsonElement>>>(json);

            if (tempMap == null) return;

            // 2. Vi transformerer listerne til de Dictionaries, som vi gerne vil bruge internt
            _rawData = new Dictionary<BuildingTypeEnum, Dictionary<int, JsonElement>>();

            foreach (var kvp in tempMap)
            {
                var levelDict = new Dictionary<int, JsonElement>();

                foreach (var element in kvp.Value)
                {
                    // Find "Level" egenskaben inde i det rå JSON-element
                    if (element.TryGetProperty("Level", out var levelProp))
                    {
                        int level = levelProp.GetInt32();
                        levelDict[level] = element;
                    }
                }

                _rawData[kvp.Key] = levelDict;
            }
        }

        public T GetConfig<T>(BuildingTypeEnum type, int level) where T : BuildingLevelData
        {
            if (_rawData.TryGetValue(type, out var levels) && levels.TryGetValue(level, out var element))
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText())!;
            }
            throw new Exception($"Bygning {type} Level {level} findes ikke i data!");
        }
    }
}
