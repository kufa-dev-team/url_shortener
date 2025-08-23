using Domain.Entities;
using Domain.Result;

namespace Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<Result<T?>> GetByIdAsync(int id);
    Task<Result<IEnumerable<T>>> GetAllAsync();
    Task<Result<T>> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task<Error?> DeleteAsync(int id);
}