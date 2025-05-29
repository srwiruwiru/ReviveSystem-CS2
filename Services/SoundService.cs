using CounterStrikeSharp.API.Core;

namespace ReviveSystem.Services
{
    public static class SoundService
    {
        public static void PlaySoundOnPlayer(CCSPlayerController? player, string sound)
        {
            if (player == null || !player.IsValid) return;
            player.ExecuteClientCommand($"play {sound}");
        }
    }
}