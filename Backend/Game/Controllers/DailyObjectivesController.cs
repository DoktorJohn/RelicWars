using Application.DTOs;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    public sealed record CollectDailyObjectiveRequest(Guid CityId);

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class DailyObjectivesController : ControllerBase
    {
        private readonly IDailyObjectiveService _service;

        public DailyObjectivesController(IDailyObjectiveService service)
        {
            _service = service;
        }

        [HttpGet("{worldPlayerId:guid}")]
        public async Task<ActionResult<DailyObjectivesDTO>> Get(Guid worldPlayerId) =>
            Ok(await _service.GetAsync(worldPlayerId));

        [HttpPost("{worldPlayerId:guid}/{definitionId:int}/collect")]
        public async Task<ActionResult<DailyObjectivesDTO>> Collect(
            Guid worldPlayerId,
            int definitionId,
            [FromBody] CollectDailyObjectiveRequest request) =>
            Ok(await _service.CollectAsync(worldPlayerId, definitionId, request.CityId));
    }
}
