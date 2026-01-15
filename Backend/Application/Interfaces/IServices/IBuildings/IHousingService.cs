using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices.IBuildings
{
    public interface IHousingService
    {
        Task<List<HousingInfoDTO>> GetHousingInfoAsync(Guid cityId);
    }
}
