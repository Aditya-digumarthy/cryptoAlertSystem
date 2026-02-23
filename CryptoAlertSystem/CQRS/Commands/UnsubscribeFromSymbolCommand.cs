// Mirror of SubscribeToSymbolCommand but records "Unsubscribe" in the audit log.
// Fix: MediatR v12+ requires IRequest<Unit> and Task<Unit> return type instead of plain IRequest/Task
using CryptoAlertSystem.Data;
using CryptoAlertSystem.Data.Entities;
using MediatR;

namespace CryptoAlertSystem.CQRS.Commands;

public record UnsubscribeFromSymbolCommand(string UserId, string Symbol) : IRequest<Unit>;

public class UnsubscribeFromSymbolCommandHandler : IRequestHandler<UnsubscribeFromSymbolCommand, Unit>
{
    private readonly AppDbContext _db;

    public UnsubscribeFromSymbolCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(UnsubscribeFromSymbolCommand request, CancellationToken cancellationToken)
    {
        var audit = new SubscriptionAudit
        {
            UserId = request.UserId,
            Symbol = request.Symbol,
            Action = "Unsubscribe",
            At = DateTime.UtcNow
        };

        _db.SubscriptionAudits.Add(audit);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value; // MediatR's way of returning "nothing" — equivalent to void
    }
}