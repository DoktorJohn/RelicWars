using Application.Services;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IResourceService
    {
        ResourceSnapshot CalculateCurrent(City city, DateTime now);
    }
}
