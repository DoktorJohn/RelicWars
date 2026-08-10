using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Enums;
using Domain.StaticData.Data;

namespace Domain.StaticData.Readers;

public sealed class EdictDataReader
{
    private IReadOnlyDictionary<EdictTypeEnum, EdictData> _definitions = new Dictionary<EdictTypeEnum, EdictData>();

    public void Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Edict definitions were not found.", path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var definitions = JsonSerializer.Deserialize<List<EdictData>>(File.ReadAllText(path), options) ?? new();
        var expected = Enum.GetValues<EdictTypeEnum>();
        if (definitions.Count != expected.Length || definitions.Select(x => x.EdictType).Distinct().Count() != expected.Length || expected.Any(x => definitions.All(d => d.EdictType != x)))
            throw new InvalidDataException("edicts.json must contain exactly one definition for every EdictTypeEnum value.");
        if (definitions.Any(x => string.IsNullOrWhiteSpace(x.Name) || string.IsNullOrWhiteSpace(x.BenefitDescription) || string.IsNullOrWhiteSpace(x.DownsideDescription)))
            throw new InvalidDataException("Every edict requires a name, benefit and downside description.");
        _definitions = definitions.ToDictionary(x => x.EdictType);
    }

    public EdictData Get(EdictTypeEnum type) => _definitions.TryGetValue(type, out var value)
        ? value
        : throw new KeyNotFoundException($"Edict {type} was not loaded.");
    public IReadOnlyCollection<EdictData> GetAll() => _definitions.Values.ToList();
}
