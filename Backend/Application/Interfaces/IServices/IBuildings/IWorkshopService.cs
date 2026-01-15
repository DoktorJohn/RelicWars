using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices.IBuildings
{
    public interface IWorkshopService
    {
        Task<WorkshopFullViewDTO> GetWorkshopOverviewAsync(Guid userId, Guid cityId);
    }
}
