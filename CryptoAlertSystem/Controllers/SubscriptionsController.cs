// SubscriptionsController exposes GET /api/subscriptions
// Returns the full audit history of all subscribe/unsubscribe actions.
// Useful for managers to see user engagement with the system.
using CryptoAlertSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptoAlertSystem.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubscriptionsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get subscription audit log. Optionally filter by userId.
    /// Example: GET /api/subscriptions?userId=user-001
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSubscriptions([FromQuery] string? userId)
    {
        var query = _db.SubscriptionAudits.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(s => s.UserId == userId);

        var results = await query
            .OrderByDescending(s => s.At)
            .Take(200)
            .ToListAsync();

        return Ok(new { count = results.Count, subscriptions = results });
    }
}