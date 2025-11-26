namespace nexthappen_backend.IAM.Application.DTOs;

public class LoginResponse
{
    public string AccessToken { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
}
