using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NextHappen.IAM.Domain.Services;

namespace NextHappen.IAM.Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _config;

    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(Guid userId, string email, string role, string fullName)
    {
        var jwtKey = _config["Jwt:Key"] ?? _config["JWT_KEY"] ?? "DefaultSuperSecretKeyForDevelopmentOnly!";
        var jwtIssuer = _config["Jwt:Issuer"] ?? _config["JWT_ISSUER"];
        var jwtAudience = _config["Jwt:Audience"] ?? _config["JWT_AUDIENCE"];

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, fullName)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
