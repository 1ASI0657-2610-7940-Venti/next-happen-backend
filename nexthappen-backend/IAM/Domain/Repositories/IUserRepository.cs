using nexthappen_backend.IAM.Domain.Entities;

namespace nexthappen_backend.IAM.Domain.Repositories;

public interface IUserRepository
{
    Task<User> GetByFullNameAndRoleAsync(string fullName, string role);
    Task<User> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task UpdateAsync(User user);
}
