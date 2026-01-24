namespace Assets.Scripts.Domain.State
{
    public struct CityResourceState
    {
        public double WoodAmount;
        public double WoodMaxCapacity;
        public double WoodProductionPerHour;

        public double StoneAmount;
        public double StoneMaxCapacity;
        public double StoneProductionPerHour;

        public double MetalAmount;
        public double MetalMaxCapacity;
        public double MetalProductionPerHour;

        public double SilverAmount;
        public double SilverProductionPerHour;

        public double ResearchPointsAmount;
        public double ResearchPointsProductionPerHour;

        public double IdeologyFocusPointsAmount;
        public double IdeologyFocusPointsProductionPerHour;

        public int CurrentPopulationUsage;
        public int MaxPopulationCapacity;
        public int FreePopulation => MaxPopulationCapacity - CurrentPopulationUsage;

        // Helper metoder til at beregne procenter til dine buer
        public float WoodFillPercentage => (float)(WoodAmount / WoodMaxCapacity);
        public float StoneFillPercentage => (float)(StoneAmount / StoneMaxCapacity);
        public float MetalFillPercentage => (float)(MetalAmount / MetalMaxCapacity);
    }
}