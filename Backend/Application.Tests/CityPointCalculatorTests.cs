using Application.Utility;

namespace Application.Tests;

public class CityPointCalculatorTests
{
    [Fact]
    public void CalculateMaximumPointsForCity_UsesEachBuildingsHighestLevelOnce()
    {
        var calculator = new CityPointCalculator(TestData.BuildingReader());

        int maximumPoints = calculator.CalculateMaximumPointsForCity();

        Assert.Equal(5_943, maximumPoints);
    }
}
