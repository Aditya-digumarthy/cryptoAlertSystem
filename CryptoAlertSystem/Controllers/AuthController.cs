// AuthController handles user login and returns a JWT token.
// In production, use ASP.NET Core Identity with hashed passwords.
// For this demo, we use hardcoded credentials to keep the focus on the architecture.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CryptoAlertSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CryptoAlertSystem.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Login and receive a JWT Bearer token.
    /// Demo credentials: admin/admin123 or user1/pass123
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Hardcoded user store — replace with DB lookup + bcrypt in production
        var validUsers = new Dictionary<string, (string Password, string UserId)>
        {
            { "admin",  ("admin123", "user-001") },
            { "user1",  ("pass123",  "user-002") },
            { "user2",  ("pass456",  "user-003") }
        };

        if (!validUsers.TryGetValue(request.Username, out var userData)
            || userData.Password != request.Password)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var token = GenerateJwtToken(userData.UserId, request.Username);

        return Ok(new
        {
            token,
            userId = userData.UserId,
            username = request.Username,
            expiresIn = 3600
        });
    }

    private string GenerateJwtToken(string userId, string username)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

        var claims = new[]
        {
            // Standard JWT claims
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            // Custom claim — used by SignalR as Context.UserIdentifier
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}