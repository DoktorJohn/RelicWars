namespace Domain.StaticData.Generators;

public static class ResearchDataGenerator
{
    public static void GenerateDefaultJson(string path)
    {
        const string resourceName = "Domain.StaticData.Defaults.research.json";
        using Stream stream = typeof(ResearchDataGenerator).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded research data '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        File.WriteAllText(path, reader.ReadToEnd());
    }
}
