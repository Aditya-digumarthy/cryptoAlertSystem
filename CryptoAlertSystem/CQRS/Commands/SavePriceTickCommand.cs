// A MediatR Command is a "write" instruction — it changes state in the database.
// This command is fired every time we receive a new price from Binance.
// Fix: MediatR v12+ requires IRequestHandler<TRequest, Unit> and Task<Unit> return type
// even for commands that don't return a meaningful value. "Unit" is MediatR's version of "void".
using CryptoAlertSystem.Data;
using CryptoAlertSystem.Data.Entities;
using MediatR;

namespace CryptoAlertSystem.CQRS.Commands;

public record SavePriceTickCommand(string Symbol, decimal Price, decimal Volume) : IRequest<Unit>;

public class SavePriceTickCommandHandler : IRequestHandler<SavePriceTickCommand, Unit>
{
    private readonly AppDbContext _db;

    public SavePriceTickCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(SavePriceTickCommand request, CancellationToken cancellationToken)
    {
        var tick = new CryptoPriceTick
        {
            Symbol = request.Symbol,
            Price = request.Price,
            Volume = request.Volume,
            Ts = DateTime.UtcNow
        };

        _db.PriceTicks.Add(tick);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value; // MediatR's way of returning "nothing" from a command
    }
}