namespace nexthappen_backend.IAM.Application.DTOs;

public class LoginRequest
{
    public string FullName { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
}
