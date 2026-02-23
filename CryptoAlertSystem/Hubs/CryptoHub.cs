// CryptoHub is the heart of real-time communication.
// SignalR groups work like chat rooms — each trading pair (BTCUSDT, ETHUSDT) is a group.
// When price updates come in, we only send them to users in the relevant group.
// [Authorize] ensures only users with a valid JWT token can connect.
using CryptoAlertSystem.CQRS.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CryptoAlertSystem.Hubs;

[Authorize] // Reject connections without a valid JWT token
public class CryptoHub : Hub
{
    private readonly IMediator _mediator;
    private readonly ILogger<CryptoHub> _logger;

    public CryptoHub(IMediator mediator, ILogger<CryptoHub> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // Called when a user connects to the hub
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? "anonymous";
        _logger.LogInformation("Client connected: {ConnectionId}, User: {UserId}",
            Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    // Called when a user disconnects (browser close, network loss, etc.)
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}, Reason: {Reason}",
            Context.ConnectionId, exception?.Message ?? "clean disconnect");
        await base.OnDisconnectedAsync(exception);
    }

    // Client calls this method to start receiving updates for a symbol
    // e.g., connection.invoke("SubscribeToSymbol", "BTCUSDT")
    public async Task SubscribeToSymbol(string symbol)
    {
        var upperSymbol = symbol.ToUpper();
        var userId = Context.UserIdentifier ?? "anonymous";

        // Add this connection to the SignalR group for this symbol
        await Groups.AddToGroupAsync(Context.ConnectionId, upperSymbol);

        // Persist the subscription action in PostgreSQL via MediatR
        await _mediator.Send(new SubscribeToSymbolCommand(userId, upperSymbol));

        _logger.LogInformation("User {UserId} subscribed to {Symbol}", userId, upperSymbol);

        // Confirm back to the client
        await Clients.Caller.SendAsync("Subscribed", upperSymbol);
    }

    // Client calls this to stop receiving updates for a symbol
    public async Task UnsubscribeFromSymbol(string symbol)
    {
        var upperSymbol = symbol.ToUpper();
        var userId = Context.UserIdentifier ?? "anonymous";

        // Remove from the SignalR group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, upperSymbol);

        // Persist the unsubscription in PostgreSQL
        await _mediator.Send(new UnsubscribeFromSymbolCommand(userId, upperSymbol));

        _logger.LogInformation("User {UserId} unsubscribed from {Symbol}", userId, upperSymbol);

        await Clients.Caller.SendAsync("Unsubscribed", upperSymbol);
    }
}