using nexthappen_backend.IAM.Application.DTOs;
using nexthappen_backend.IAM.Domain.Entities;
using nexthappen_backend.IAM.Domain.Repositories;
using nexthappen_backend.IAM.Domain.Services;

namespace nexthappen_backend.IAM.Application.UseCases;

public class RegisterUser
{
    private readonly IUserRepository _repo;
    private readonly IPasswordHasher _hasher;

    public RegisterUser(IUserRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task HandleAsync(RegisterRequest request)
    {
        var existing = await _repo.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new Exception("Email already exists");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Role = request.Role,
            PasswordHash = _hasher.Hash(request.Password)
        };

        await _repo.AddAsync(user);
    }
}
