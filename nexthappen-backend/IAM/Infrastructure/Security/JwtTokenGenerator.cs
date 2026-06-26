using System.Security.Claims;
using System.Text;
using nexthappen_backend.IAM.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;


namespace nexthappen_backend.IAM.Infrastructure.Security;

public class JwtTokenGenerator
{
    private readonly IConfiguration _config;

    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;
    }

    public string Generate(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? _config["JWT_KEY"] ?? "DefaultSuperSecretKeyForDevelopmentOnly!";
        var jwtIssuer = _config["Jwt:Issuer"] ?? _config["JWT_ISSUER"];
        var jwtAudience = _config["Jwt:Audience"] ?? _config["JWT_AUDIENCE"];

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("id", user.Id.ToString()),
            new Claim("role", user.Role),
            new Claim("fullName", user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(3),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
