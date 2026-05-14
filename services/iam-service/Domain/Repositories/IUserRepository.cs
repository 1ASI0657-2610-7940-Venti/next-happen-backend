using NextHappen.IAM.Domain.Entities;

namespace NextHappen.IAM.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByFullNameAndRoleAsync(string fullName, string role);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
