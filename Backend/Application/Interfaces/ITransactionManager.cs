using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ITransactionManager
    {
        bool HasActiveTransaction => false;
        Task ExecuteAsync(Func<Task> operation);
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
        Task SaveChangesAsync() => Task.CompletedTask;
    }
}
