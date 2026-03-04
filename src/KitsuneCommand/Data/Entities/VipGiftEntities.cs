namespace KitsuneCommand.Data.Entities
{
    /// <summary>
    /// Maps to the vip_gifts SQLite table. Admin-assigned gift packages for VIP players.
    /// </summary>
    public class VipGift
    {
        public int Id { get; set; }
        public string CreatedAt { get; set; }
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Name { get; set; }
        public int Claimed { get; set; }
        public int TotalClaimCount { get; set; }
        public string LastClaimedAt { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// null = one-time gift, "daily" / "weekly" / "monthly" = repeatable.
        /// </summary>
        public string ClaimPeriod { get; set; }
    }
}
