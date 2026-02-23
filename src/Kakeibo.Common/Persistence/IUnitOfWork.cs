namespace Kakeibo.Common.Persistence;

// Unit of work abstraction
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
