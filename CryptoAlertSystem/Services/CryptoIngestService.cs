// CryptoIngestService is a BackgroundService — it runs forever in the background.
// It connects to Binance's public WebSocket stream and receives live price ticks.
// The latest price for each symbol is stored in a ConcurrentDictionary (in-memory cache).
// It also fires SavePriceTickCommand every tick to persist data in PostgreSQL.
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CryptoAlertSystem.CQRS.Commands;
using CryptoAlertSystem.Models;
using MediatR;

namespace CryptoAlertSystem.Services;

public class CryptoIngestService : BackgroundService
{
    private readonly ILogger<CryptoIngestService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    // This is our in-memory price cache — shared with CryptoBroadcastService
    // ConcurrentDictionary is thread-safe, critical because multiple threads access it
    public static readonly ConcurrentDictionary<string, (decimal Price, decimal Volume)>
        LatestPrices = new();

    // Binance combined stream URL — subscribes to all mini-ticker streams at once
    // !miniTicker@arr gives us price updates for ALL symbols every second
    private const string BinanceWsUrl =
        "wss://stream.binance.com:9443/ws/!miniTicker@arr";

    public CryptoIngestService(
        ILogger<CryptoIngestService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Keep reconnecting if the WebSocket drops
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndIngest(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket ingestion error. Reconnecting in 5s...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ConnectAndIngest(CancellationToken stoppingToken)
    {
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(BinanceWsUrl), stoppingToken);
        _logger.LogInformation("Connected to Binance WebSocket stream");

        var buffer = new byte[64 * 1024]; // 64KB buffer for incoming messages

        while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogWarning("Binance WebSocket closed by server");
                break;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            await ProcessMessage(json, stoppingToken);
        }
    }

    private async Task ProcessMessage(string json, CancellationToken stoppingToken)
    {
        try
        {
            // Binance sends an array of ticker objects
            var tickers = JsonSerializer.Deserialize<List<BinanceTickerMessage>>(json);
            if (tickers == null) return;

            // We only care about the most common USDT pairs to avoid flooding the DB
            var watchedSymbols = new HashSet<string>
                { "BTCUSDT", "ETHUSDT", "BNBUSDT", "SOLUSDT", "XRPUSDT" };

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            foreach (var ticker in tickers)
            {
                if (!watchedSymbols.Contains(ticker.Symbol)) continue;

                if (!decimal.TryParse(ticker.CurrentPrice, out var price)) continue;
                if (!decimal.TryParse(ticker.Volume, out var volume)) continue;

                // Update the in-memory cache — broadcast service reads from here
                LatestPrices[ticker.Symbol] = (price, volume);

                // Persist to PostgreSQL via CQRS command
                await mediator.Send(
                    new SavePriceTickCommand(ticker.Symbol, price, volume),
                    stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Binance message");
        }
    }
}