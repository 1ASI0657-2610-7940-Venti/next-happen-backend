namespace nexthappen_backend.IAM.Application.DTOs;

public class RegisterRequest
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; } // "User" o "Organizer"
}
