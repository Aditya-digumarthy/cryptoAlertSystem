// This command is triggered when a user subscribes to a trading pair via SignalR.
// It saves an audit record to PostgreSQL so managers can see subscription history.
// Fix: MediatR v12+ requires IRequest<Unit> and Task<Unit> return type instead of plain IRequest/Task
using CryptoAlertSystem.Data;
using CryptoAlertSystem.Data.Entities;
using MediatR;

namespace CryptoAlertSystem.CQRS.Commands;

public record SubscribeToSymbolCommand(string UserId, string Symbol) : IRequest<Unit>;

public class SubscribeToSymbolCommandHandler : IRequestHandler<SubscribeToSymbolCommand, Unit>
{
    private readonly AppDbContext _db;

    public SubscribeToSymbolCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(SubscribeToSymbolCommand request, CancellationToken cancellationToken)
    {
        var audit = new SubscriptionAudit
        {
            UserId = request.UserId,
            Symbol = request.Symbol,
            Action = "Subscribe",
            At = DateTime.UtcNow
        };

        _db.SubscriptionAudits.Add(audit);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value; // MediatR's way of returning "nothing" — equivalent to void
    }
}