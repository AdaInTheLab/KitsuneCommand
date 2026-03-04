namespace KitsuneCommand.Features
{
    /// <summary>
    /// Configuration settings for the Store feature.
    /// </summary>
    public class StoreSettings
    {
        /// <summary>Cooldown in seconds between purchases for a player.</summary>
        public int PurchaseCooldownSeconds { get; set; } = 0;

        /// <summary>Maximum purchases per player per day (0 = unlimited).</summary>
        public int MaxDailyPurchases { get; set; } = 0;

        /// <summary>Global price multiplier applied to all goods (1.0 = normal).</summary>
        public double PriceMultiplier { get; set; } = 1.0;
    }
}
