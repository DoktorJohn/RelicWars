using Domain.Entities;

namespace Application.Interfaces.IServices
{
    public interface IResistanceService
    {
        double CalculateRecoveryPerHour(City city);
        void UpdateResistance(City city, DateTime now);
    }
}
