using ReviveSystem.Models;

namespace ReviveSystem.Managers
{
    public static class PlayerManager
    {
        public static Dictionary<int, PlayerInfo> PlayersInfo { get; } = new();

        public static void ResetAllPlayersForNewRound()
        {
            foreach (var player in PlayersInfo.Values)
            {
                player.DiePosition = null;
                player.ReviveCount = 0;
            }
        }

        public static void EnsurePlayerExists(int userId, string playerName)
        {
            if (!PlayersInfo.ContainsKey(userId))
            {
                PlayersInfo[userId] = new PlayerInfo(userId, playerName);
            }
        }

        public static void TrackNewPlayer(int userId, string playerName)
        {
            PlayersInfo[userId] = new PlayerInfo(userId, playerName);
        }

        public static void StopTrackingPlayer(int userId)
        {
            PlayersInfo.Remove(userId);
        }
    }
}