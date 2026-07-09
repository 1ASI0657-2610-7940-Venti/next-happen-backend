namespace NextHappen.IAM.Domain.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, string role, string fullName);
}