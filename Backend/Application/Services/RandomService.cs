using Application.Interfaces.IServices;

namespace Application.Services
{
    public class RandomService : IRandomService
    {
        public int Next(int maxValue) => Random.Shared.Next(maxValue);
        public double NextDouble() => Random.Shared.NextDouble();
    }
}
