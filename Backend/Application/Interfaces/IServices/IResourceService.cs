using Application.Services;
using Domain.Entities;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IResourceService
    {
        CityResourceSnapshot CalculateCityResources(City cityEntity, DateTime currentDateTime);
        CityProductionSnapshot CalculateCityProduction(WorldPlayer playerEntity, City cityEntity);
        GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer playerEntity, DateTime currentDateTime);
    }
}
