using Microsoft.AspNetCore.Mvc;
using nexthappen_backend.IAM.Application.DTOs;
using nexthappen_backend.IAM.Application.UseCases;

namespace nexthappen_backend.IAM.API;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUser _registerUser;
    private readonly LoginUser _loginUser;

    public AuthController(RegisterUser registerUser, LoginUser loginUser)
    {
        _registerUser = registerUser;
        _loginUser = loginUser;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        await _registerUser.HandleAsync(request);
        return Ok(new { message = "User created successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var response = await _loginUser.HandleAsync(request);
        return Ok(response);
    }
}
