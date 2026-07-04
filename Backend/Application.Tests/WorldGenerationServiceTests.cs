using Domain.Enums;
using Domain.StaticData.Generators;

namespace Application.Tests;

public class WorldGenerationServiceTests
{
    private static readonly (int X, int Y)[] NeighborOffsets =
    {
        (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1)
    };

    [Fact]
    public void CalculateWorldMapBiomeVariant_IsDeterministicForKnownSeed()
    {
        const int seed = 42069;
        var firstPass = SampleArea(seed);
        var secondPass = SampleArea(seed);

        Assert.Equal(firstPass, secondPass);
        Assert.Contains(WorldBiomeVariantType.Ocean_1, firstPass);
        Assert.Contains(firstPass, biome => biome < WorldBiomeVariantType.Ocean_1);
    }

    [Fact]
    public void GeneratedIslands_HaveAtLeastFiveOceanTilesBetweenLandGroups()
    {
        const int seed = 42069;
        var land = Coordinates(-100, 100)
            .Where(coordinate => WorldGenerationService.IsLand(coordinate.X, coordinate.Y, seed))
            .ToHashSet();
        var islands = land
            .Select(coordinate =>
            {
                bool found = WorldGenerationService.TryGetIslandCoordinates(
                    coordinate.X, coordinate.Y, seed, out int islandX, out int islandY);
                Assert.True(found);
                return (Coordinate: coordinate, Island: (X: islandX, Y: islandY));
            })
            .GroupBy(entry => entry.Island)
            .Select(group => group.Select(entry => entry.Coordinate).ToHashSet())
            .ToList();

        Assert.True(islands.Count > 4);
        for (int first = 0; first < islands.Count; first++)
        {
            for (int second = first + 1; second < islands.Count; second++)
            {
                int closestDistance = islands[first]
                    .SelectMany(a => islands[second].Select(b => HexDistance(a.X - b.X, a.Y - b.Y)))
                    .Min();
                Assert.True(closestDistance >= 6, $"Islands were only {closestDistance - 1} ocean tiles apart.");
            }
        }

        var interiorIslands = islands
            .Where(island =>
            {
                var coordinate = island.First();
                WorldGenerationService.TryGetIslandCoordinates(
                    coordinate.X, coordinate.Y, seed, out int islandX, out int islandY);
                var definition = WorldGenerationService.GetIslandDefinition(islandX, islandY, seed);
                return Math.Abs(definition.CenterX) <= 80 && Math.Abs(definition.CenterY) <= 80;
            })
            .ToList();
        var nearestIslandDistances = interiorIslands.Select(island => islands
            .Where(other => !ReferenceEquals(other, island))
            .Select(other => island
                .SelectMany(a => other.Select(b => HexDistance(a.X - b.X, a.Y - b.Y)))
                .Min())
            .Min());
        int largestNearestDistance = nearestIslandDistances.Max();
        Assert.True(largestNearestDistance <= 20, $"The generated area contains an ocean gap of {largestNearestDistance - 1} tiles.");
    }

    [Fact]
    public void IsCoastal_MatchesLandTilesTouchingOcean()
    {
        const int seed = 42069;

        foreach (var coordinate in Coordinates(-60, 60))
        {
            bool expected = WorldGenerationService.IsLand(coordinate.X, coordinate.Y, seed)
                && GetNeighborOffsets(coordinate.Y).Any(offset => !WorldGenerationService.IsLand(
                    coordinate.X + offset.X,
                    coordinate.Y + offset.Y,
                    seed));

            Assert.Equal(expected, WorldGenerationService.IsCoastal(coordinate.X, coordinate.Y, seed));
        }
    }

    private static (int X, int Y)[] GetNeighborOffsets(int y) => (y & 1) == 0
        ? [(1, 0), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1)]
        : [(1, 0), (1, 1), (0, 1), (-1, 0), (0, -1), (1, -1)];

    [Fact]
    public void IslandCoast_HasCapacityForApproximatelyTwentyFiveCities()
    {
        const int seed = 42069;
        var activeCell = Coordinates(-10, 10)
            .First(cell => WorldGenerationService.IsIslandCellActive(cell.X, cell.Y, seed));
        var island = WorldGenerationService.GetIslandDefinition(activeCell.X, activeCell.Y, seed);
        int searchRadius = WorldGenerationService.MaximumIslandRadius + 1;
        var coastalTiles = CoordinatesAround(island.CenterX, island.CenterY, searchRadius)
            .Where(coordinate => WorldGenerationService.TryGetIslandCoordinates(
                coordinate.X, coordinate.Y, seed, out int islandX, out int islandY)
                && islandX == activeCell.X
                && islandY == activeCell.Y
                && WorldGenerationService.IsCoastal(coordinate.X, coordinate.Y, seed))
            .ToList();

        Assert.True(coastalTiles.Count >= 25, $"The island only exposed {coastalTiles.Count} coastal tiles.");
    }

    [Fact]
    public void IslandDefinitions_IncludeRoundLongOvalAndIrregularShapes()
    {
        const int seed = 42069;
        var definitions = Coordinates(-5, 5)
            .Select(cell => WorldGenerationService.GetIslandDefinition(cell.X, cell.Y, seed))
            .ToList();

        Assert.Equal(new[] { 0, 1, 2, 3 }, definitions.Select(island => island.Shape).Distinct().Order().ToArray());
        Assert.True(definitions.Select(island => island.RotationDegrees).Distinct().Count() > 20);
    }

    [Fact]
    public void TryGetIslandCoordinates_ReturnsStableIslandIdentity()
    {
        const int seed = 42069;
        var activeCell = Coordinates(-10, 10)
            .First(cell => WorldGenerationService.IsIslandCellActive(cell.X, cell.Y, seed));
        var island = WorldGenerationService.GetIslandDefinition(activeCell.X, activeCell.Y, seed);

        Assert.True(WorldGenerationService.TryGetIslandCoordinates(
            island.CenterX, island.CenterY, seed, out int firstX, out int firstY));
        Assert.True(WorldGenerationService.TryGetIslandCoordinates(
            island.CenterX, island.CenterY, seed, out int secondX, out int secondY));
        Assert.Equal(activeCell, (firstX, firstY));
        Assert.Equal((firstX, firstY), (secondX, secondY));
    }

    [Fact]
    public void ActiveIslandCenters_AreScatteredAndRespectMinimumSpacing()
    {
        const int seed = 42069;
        var islands = Coordinates(-15, 15)
            .Where(cell => WorldGenerationService.IsIslandCellActive(cell.X, cell.Y, seed))
            .Select(cell => WorldGenerationService.GetIslandDefinition(cell.X, cell.Y, seed))
            .ToList();

        Assert.True(islands.Count > 20);
        Assert.True(islands.Select(island => PositiveModulo(island.CenterX, WorldGenerationService.IslandCellSize)).Distinct().Count() > 10);
        Assert.True(islands.Select(island => PositiveModulo(island.CenterY, WorldGenerationService.IslandCellSize)).Distinct().Count() > 10);

        float closestDistance = islands
            .SelectMany((first, index) => islands.Skip(index + 1).Select(second => CartesianDistance(first, second)))
            .Min();
        Assert.True(closestDistance >= 18f, $"Island centers were only {closestDistance:F1} visual tiles apart.");
    }

    private static List<WorldBiomeVariantType> SampleArea(int seed) =>
        Coordinates(-50, 50)
            .Select(coordinate => WorldGenerationService.CalculateWorldMapBiomeVariant(
                (short)coordinate.X,
                (short)coordinate.Y,
                seed))
            .ToList();

    private static IEnumerable<(int X, int Y)> Coordinates(int minimum, int maximum)
    {
        for (int x = minimum; x <= maximum; x++)
        for (int y = minimum; y <= maximum; y++)
            yield return (x, y);
    }

    private static IEnumerable<(int X, int Y)> CoordinatesAround(int centerX, int centerY, int radius)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
        for (int y = centerY - radius; y <= centerY + radius; y++)
            yield return (x, y);
    }

    private static int HexDistance(int deltaX, int deltaY) =>
        Math.Max(Math.Abs(deltaX), Math.Max(Math.Abs(deltaY), Math.Abs(deltaX + deltaY)));

    private static int PositiveModulo(int value, int divisor) => (value % divisor + divisor) % divisor;

    private static float CartesianDistance(
        WorldGenerationService.IslandDefinition first,
        WorldGenerationService.IslandDefinition second)
    {
        float firstCenterX = first.CenterX + (((first.CenterY & 1) != 0) ? 0.5f : 0f);
        float secondCenterX = second.CenterX + (((second.CenterY & 1) != 0) ? 0.5f : 0f);
        float cartesianX = firstCenterX - secondCenterX;
        float deltaY = first.CenterY - second.CenterY;
        float cartesianY = deltaY * 0.8660254f;
        return MathF.Sqrt(cartesianX * cartesianX + cartesianY * cartesianY);
    }
}
