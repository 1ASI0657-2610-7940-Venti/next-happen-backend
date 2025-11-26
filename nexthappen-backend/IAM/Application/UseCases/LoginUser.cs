using nexthappen_backend.IAM.Application.DTOs;
using nexthappen_backend.IAM.Domain.Repositories;
using nexthappen_backend.IAM.Domain.Services;
using nexthappen_backend.IAM.Infrastructure.Security;

namespace nexthappen_backend.IAM.Application.UseCases;

public class LoginUser
{
    private readonly IUserRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly JwtTokenGenerator _tokenGen;

    public LoginUser(IUserRepository repo, IPasswordHasher hasher, JwtTokenGenerator tokenGen)
    {
        _repo = repo;
        _hasher = hasher;
        _tokenGen = tokenGen;
    }

    public async Task<LoginResponse> HandleAsync(LoginRequest request)
    {
        var user = await _repo.GetByFullNameAndRoleAsync(request.FullName, request.Role);

        if (user == null)
            throw new Exception("User does not exist");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid password");

        var token = _tokenGen.Generate(user);

        return new LoginResponse
        {
            AccessToken = token,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}
