// AppDbContext is the EF Core "bridge" between our C# classes and PostgreSQL tables.
// EF Core reads our entity classes and automatically creates/manages the DB schema.
using CryptoAlertSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoAlertSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // These two properties represent our two database tables
    public DbSet<CryptoPriceTick> PriceTicks => Set<CryptoPriceTick>();
    public DbSet<SubscriptionAudit> SubscriptionAudits => Set<SubscriptionAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map entity class names to PostgreSQL snake_case table names (best practice)
        modelBuilder.Entity<CryptoPriceTick>().ToTable("crypto_price_ticks");
        modelBuilder.Entity<SubscriptionAudit>().ToTable("subscription_audits");

        // Index on Symbol + Ts so queries like "last 50 ticks for BTCUSDT" are fast
        modelBuilder.Entity<CryptoPriceTick>()
            .HasIndex(x => new { x.Symbol, x.Ts });
    }
}