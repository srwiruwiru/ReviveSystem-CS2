using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ReviveSystem.Utils
{
    public static class PlayerUtils
    {
        public static bool IsPlayerOnGround(CCSPlayerController player)
        {
            if (player.PlayerPawn?.Value == null)
                return false;

            var playerFlags = player.PlayerPawn.Value.Flags;
            return (playerFlags & (uint)PlayerFlags.FL_ONGROUND) == (uint)PlayerFlags.FL_ONGROUND;
        }

        public static bool TryParseButton(string buttonName, out PlayerButtons button)
        {
            return Enum.TryParse(buttonName, ignoreCase: true, out button) && Enum.IsDefined(button);
        }
    }
}