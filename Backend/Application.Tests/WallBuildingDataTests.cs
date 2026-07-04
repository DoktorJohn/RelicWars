using System.Linq;
using Domain.Enums;
using Domain.StaticData.Data;

namespace Application.Tests;

public class WallBuildingDataTests
{
    [Fact]
    public void WallBonusProgressionStartsAtFivePercentAndEndsAt111Percent()
    {
        var reader = TestData.BuildingReader();

        var levelOne = reader.GetConfig<WallLevelData>(BuildingTypeEnum.Wall, 1);
        var levelTwenty = reader.GetConfig<WallLevelData>(BuildingTypeEnum.Wall, 20);

        double levelOneBonus = levelOne.ModifiersInternal.Single(modifier => modifier.Tag == ModifierTagEnum.Wall).Value;
        double levelTwentyBonus = levelTwenty.ModifiersInternal.Single(modifier => modifier.Tag == ModifierTagEnum.Wall).Value;

        Assert.Equal(0.05, levelOneBonus, 3);
        Assert.Equal(1.11, levelTwentyBonus, 3);
    }

    [Fact]
    public void WallBonusProgressionIsStrictlyIncreasing()
    {
        var reader = TestData.BuildingReader();
        double previousBonus = 0;

        for (int level = 1; level <= 20; level++)
        {
            var config = reader.GetConfig<WallLevelData>(BuildingTypeEnum.Wall, level);
            double currentBonus = config.ModifiersInternal.Single(modifier => modifier.Tag == ModifierTagEnum.Wall).Value;

            Assert.True(currentBonus > previousBonus, $"Level {level} bonus {currentBonus} was not greater than {previousBonus}.");
            previousBonus = currentBonus;
        }
    }
}
