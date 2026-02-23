// A MediatR Query is a "read" operation — it fetches data without changing anything.
// This query returns the last N price ticks for a given symbol from PostgreSQL.
// Used by GET /api/ticks/{symbol}?limit=50
using CryptoAlertSystem.Data;
using CryptoAlertSystem.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CryptoAlertSystem.CQRS.Queries;

// The query carries the symbol name and how many records to return
public record GetRecentTicksQuery(string Symbol, int Limit) : IRequest<List<CryptoPriceTick>>;

public class GetRecentTicksQueryHandler : IRequestHandler<GetRecentTicksQuery, List<CryptoPriceTick>>
{
    private readonly AppDbContext _db;

    public GetRecentTicksQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CryptoPriceTick>> Handle(
        GetRecentTicksQuery request,
        CancellationToken cancellationToken)
    {
        // Order by newest first, take N records, return as list
        return await _db.PriceTicks
            .Where(t => t.Symbol == request.Symbol.ToUpper())
            .OrderByDescending(t => t.Ts)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);
    }
}