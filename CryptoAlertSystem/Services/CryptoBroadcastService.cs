// CryptoBroadcastService is the second BackgroundService.
// Every 500ms it reads the latest prices from the in-memory cache
// and broadcasts them to all SignalR clients subscribed to each symbol's group.
// This decouples ingestion speed (Binance pushes ~1s) from broadcast cadence (500ms).
using CryptoAlertSystem.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CryptoAlertSystem.Services;

public class CryptoBroadcastService : BackgroundService
{
    private readonly IHubContext<CryptoHub> _hubContext;
    private readonly ILogger<CryptoBroadcastService> _logger;

    // Broadcast every 500 milliseconds as required
    private static readonly TimeSpan BroadcastInterval = TimeSpan.FromMilliseconds(500);

    public CryptoBroadcastService(
        IHubContext<CryptoHub> hubContext,
        ILogger<CryptoBroadcastService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CryptoBroadcastService started — broadcasting every 500ms");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(BroadcastInterval, stoppingToken);
            await BroadcastPrices(stoppingToken);
        }
    }

    private async Task BroadcastPrices(CancellationToken stoppingToken)
    {
        // Read from the shared in-memory cache populated by CryptoIngestService
        var prices = CryptoIngestService.LatestPrices;

        if (prices.IsEmpty) return;

        foreach (var (symbol, data) in prices)
        {
            // Send to the SignalR group named after the symbol
            // Only clients who called SubscribeToSymbol("BTCUSDT") will receive BTCUSDT updates
            await _hubContext.Clients.Group(symbol).SendAsync(
                "PriceUpdate",                          // Method name the client listens to
                new { symbol, data.Price, data.Volume, timestamp = DateTime.UtcNow },
                stoppingToken);

            _logger.LogDebug("Broadcast {Symbol} @ {Price} to group", symbol, data.Price);
        }
    }
}