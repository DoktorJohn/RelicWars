using Application.DTOs;
using Domain.Enums;

namespace Application.Interfaces.IServices;

public interface IEdictService
{
    Task<EdictOverviewDTO> GetOverviewAsync(Guid cityId);
    Task<EdictOverviewDTO> EnactAsync(Guid cityId, EdictTypeEnum edictType);
}
