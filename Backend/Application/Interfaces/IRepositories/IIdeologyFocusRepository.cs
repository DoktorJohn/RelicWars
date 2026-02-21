using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IIdeologyFocusRepository
    {
        public Task<List<IdeologyFocus>?> GetAll();
        public Task<List<IdeologyFocus>?> GetAllActive();
        public Task<List<IdeologyFocus>?> GetAllByCityPlayer(Guid cityId);
        public Task UpdateAsync(IdeologyFocus ideologyFocus);
        public Task AddAsync(IdeologyFocus ideologyFocus);
        public Task DeleteExpiredFocusesForCityAsync(Guid cityId);
    }
}
