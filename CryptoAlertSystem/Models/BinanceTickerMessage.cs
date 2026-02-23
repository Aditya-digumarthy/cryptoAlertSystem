// This maps the JSON structure that Binance WebSocket sends us.
// Binance sends messages like: { "s": "BTCUSDT", "c": "67000.00", "v": "12345.67" }
// We use System.Text.Json property name attributes to match Binance's field names exactly.
using System.Text.Json.Serialization;

namespace CryptoAlertSystem.Models;

public class BinanceTickerMessage
{
    [JsonPropertyName("s")]
    public string Symbol { get; set; } = string.Empty;   // Symbol

    [JsonPropertyName("c")]
    public string CurrentPrice { get; set; } = string.Empty; // Close/Current price

    [JsonPropertyName("v")]
    public string Volume { get; set; } = string.Empty;    // Volume
}