namespace ReviveSystem.Models
{
    public class PlayerInfo
    {
        public int? UserId { get; }
        public string Name { get; }
        public DiePosition? DiePosition { get; set; }
        public int ReviveCount { get; set; } = 0;
        public DateTime? UseStartTime { get; set; }
        public DateTime? LastReviveLimitMessageTime { get; set; }

        public PlayerInfo(int? userId, string name)
        {
            UserId = userId;
            Name = name;
            DiePosition = null;
            UseStartTime = null;
            LastReviveLimitMessageTime = null;
        }
    }
}