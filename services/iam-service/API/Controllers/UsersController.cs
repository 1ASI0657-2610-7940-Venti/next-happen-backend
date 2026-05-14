using Microsoft.AspNetCore.Mvc;
using NextHappen.IAM.Application.DTOs;
using NextHappen.IAM.Domain.Repositories;

namespace NextHappen.IAM.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;

    public UsersController(IUserRepository users)
    {
        _users = users;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.AvatarUrl
        });
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrWhiteSpace(request.Email))
            user.Email = request.Email;

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl;

        await _users.UpdateAsync(user);

        return Ok(new
        {
            message = "Perfil actualizado correctamente",
            user = new { user.Id, user.FullName, user.Email, user.Role, user.AvatarUrl }
        });
    }
}
