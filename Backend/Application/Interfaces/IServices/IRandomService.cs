namespace Application.Interfaces.IServices
{
    public interface IRandomService
    {
        int Next(int maxValue);
        double NextDouble();
    }
}
