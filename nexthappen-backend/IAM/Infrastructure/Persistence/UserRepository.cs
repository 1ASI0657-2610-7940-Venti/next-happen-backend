using nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration;
using nexthappen_backend.IAM.Domain.Entities;
using nexthappen_backend.IAM.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace nexthappen_backend.IAM.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<User?> GetByFullNameAndRoleAsync(string fullName, string role)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.FullName == fullName && x.Role == role);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
    
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}