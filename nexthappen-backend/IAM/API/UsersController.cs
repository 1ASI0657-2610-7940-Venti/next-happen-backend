using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.IAM.Application.DTOs;
using nexthappen_backend.IAM.Domain.Repositories;

namespace nexthappen_backend.IAM.API;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;

    public UsersController(IUserRepository users)
    {
        _users = users;
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
            user
        });
    }
}