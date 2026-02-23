namespace CryptoAlertSystem.Data.Entities
{
    public class CryptoPriceTick
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Trading pair symbol e.g. BTCUSDT, ETHUSDT
        public string Symbol { get; set; } = string.Empty;

        // The actual price at this moment
        public decimal Price { get; set; }

        // 24h traded volume from Binance
        public decimal Volume { get; set; }

        // Exact UTC timestamp when this tick was received
        public DateTime Ts { get; set; } = DateTime.UtcNow;
    }
}

// This entity maps to the "crypto_price_ticks" table in PostgreSQL.
// It stores every live price update received from Binance WebSocket.