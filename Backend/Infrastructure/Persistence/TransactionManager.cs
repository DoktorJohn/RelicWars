using Application.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class TransactionManager : ITransactionManager
    {
        private readonly GameContext _context;

        public TransactionManager(GameContext context)
        {
            _context = context;
        }

        public Task ExecuteAsync(Func<Task> operation)
        {
            return ExecuteAsync(async () =>
            {
                await operation();
                return true;
            });
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (_context.Database.CurrentTransaction != null)
            {
                return await operation();
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            });
        }
    }
}
