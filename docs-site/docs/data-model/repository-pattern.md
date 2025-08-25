---
id: repository-pattern
title: Repository Pattern
---

`IRepository<T>` abstracts data access. See `src/Domain/Interfaces/IRepository.cs` and Infrastructure implementations.

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}
```
