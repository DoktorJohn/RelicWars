using System;

namespace Application.Interfaces.IServices
{
    public interface ICurrentUserService
    {
        Guid GetProfileId();
        bool TryGetProfileId(out Guid profileId);
    }
}
