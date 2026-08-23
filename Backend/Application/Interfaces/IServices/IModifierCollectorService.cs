using Domain.Abstraction;
using Domain.Entities;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IModifierCollectorService
    {
        List<IModifierProvider> CollectAllProvidersForCity(City cityEntity);
        List<IModifierProvider> CollectAllProvidersForCity(City cityEntity, WorldPlayer playerEntity, DateTime asOfUtc);
        List<IModifierProvider> CollectAllProvidersForPlayer(WorldPlayer playerEntity);
    }
}
