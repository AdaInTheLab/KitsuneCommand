using KitsuneCommand.Data.Entities;

namespace KitsuneCommand.Web.Models
{
    public class VipGiftDetailResponse
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
        public string ClaimPeriod { get; set; }
        public bool IsClaimable { get; set; }
        public List<ItemDefinition> Items { get; set; } = new List<ItemDefinition>();
        public List<CommandDefinition> Commands { get; set; } = new List<CommandDefinition>();
    }

    public class CreateVipGiftRequest
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ClaimPeriod { get; set; }
        public List<int> ItemIds { get; set; } = new List<int>();
        public List<int> CommandIds { get; set; } = new List<int>();
    }

    public class UpdateVipGiftRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ClaimPeriod { get; set; }
        public string PlayerName { get; set; }
        public List<int> ItemIds { get; set; }
        public List<int> CommandIds { get; set; }
    }
}
