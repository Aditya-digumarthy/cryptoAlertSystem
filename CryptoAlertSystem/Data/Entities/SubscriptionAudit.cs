namespace CryptoAlertSystem.Data.Entities
{
    public class SubscriptionAudit
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // The user's identity (from JWT token claim)
        public string UserId { get; set; } = string.Empty;

        // Which trading pair they acted on
        public string Symbol { get; set; } = string.Empty;

        // "Subscribe" or "Unsubscribe"
        public string Action { get; set; } = string.Empty;

        // When this action happened
        public DateTime At { get; set; } = DateTime.UtcNow;
    }
}


// This entity maps to "subscription_audits" table.
// Every time a user subscribes or unsubscribes to a symbol, we record it here.
// This gives us a full audit trail — who subscribed to what and when.