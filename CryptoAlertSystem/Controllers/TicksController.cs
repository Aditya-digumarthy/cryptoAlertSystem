// TicksController exposes the GET /api/ticks/{symbol} endpoint.
// Uses the MediatR Query pattern to fetch recent price history from PostgreSQL.
using CryptoAlertSystem.CQRS.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoAlertSystem.Controllers;

[ApiController]
[Route("api/ticks")]
[Authorize]
public class TicksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the last N price ticks for a given symbol.
    /// Example: GET /api/ticks/BTCUSDT?limit=50
    /// </summary>
    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetRecentTicks(
        string symbol,
        [FromQuery] int limit = 50)
    {
        if (limit > 500) limit = 500; // Cap to prevent huge queries

        // Dispatch the query through MediatR — handler fetches from PostgreSQL
        var ticks = await _mediator.Send(new GetRecentTicksQuery(symbol, limit));

        return Ok(new
        {
            symbol = symbol.ToUpper(),
            count = ticks.Count,
            ticks
        });
    }
}