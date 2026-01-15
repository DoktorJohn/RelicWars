using Application.DTOs;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices.IBuildings
{
    public interface IAcademyService
    {
        Task<List<AcademyInfoDTO>> GetAcademyInfoAsync(Guid cityId);
        
    }
}
