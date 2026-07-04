using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Game.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid GetProfileId()
        {
            if (TryGetProfileId(out var profileId))
            {
                return profileId;
            }

            throw new UnauthorizedAccessException("Ugyldigt eller manglende autentificeret bruger-ID i token.");
        }

        public bool TryGetProfileId(out Guid profileId)
        {
            profileId = Guid.Empty;

            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out profileId);
        }
    }
}
