using NextHappen.IAM.Application.DTOs;
using NextHappen.IAM.Domain.Repositories;
using NextHappen.IAM.Domain.Services;
using NextHappen.IAM.Infrastructure.Security;

namespace NextHappen.IAM.Application.UseCases;

public class LoginUser
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _tokenGenerator;

    public LoginUser(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        JwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponse> HandleAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new Exception("Credenciales inválidas.");

        var token = _tokenGenerator.GenerateToken(user.Id, user.Email, user.Role, user.FullName);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Token = token,
            AvatarUrl = user.AvatarUrl
        };
    }
}
