// Simple DTO (Data Transfer Object) that the client sends in POST /api/auth/login
// We keep auth simple with hardcoded users for this demo — in production use ASP.NET Identity
namespace CryptoAlertSystem.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}