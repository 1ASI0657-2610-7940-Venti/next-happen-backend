using Microsoft.EntityFrameworkCore;
using NextHappen.IAM.Domain.Entities;
using NextHappen.IAM.Domain.Repositories;

namespace NextHappen.IAM.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IamDbContext _context;

    public UserRepository(IamDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByFullNameAndRoleAsync(string fullName, string role)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.FullName == fullName && x.Role == role);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
