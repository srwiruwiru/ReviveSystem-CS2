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

        public static PlayerButtons ParseReviveButton(string buttonName)
        {
            return buttonName.ToUpper() switch
            {
                "ATTACK" => PlayerButtons.Attack,
                "JUMP" => PlayerButtons.Jump,
                "DUCK" => PlayerButtons.Duck,
                "FORWARD" => PlayerButtons.Forward,
                "BACK" => PlayerButtons.Back,
                "USE" => PlayerButtons.Use,
                "CANCEL" => PlayerButtons.Cancel,
                "LEFT" => PlayerButtons.Left,
                "RIGHT" => PlayerButtons.Right,
                "MOVELEFT" => PlayerButtons.Moveleft,
                "MOVERIGHT" => PlayerButtons.Moveright,
                "ATTACK2" => PlayerButtons.Attack2,
                "RUN" => PlayerButtons.Run,
                "RELOAD" => PlayerButtons.Reload,
                "ALT1" => PlayerButtons.Alt1,
                "ALT2" => PlayerButtons.Alt2,
                "SPEED" => PlayerButtons.Speed,
                "WALK" => PlayerButtons.Walk,
                "ZOOM" => PlayerButtons.Zoom,
                "WEAPON1" => PlayerButtons.Weapon1,
                "WEAPON2" => PlayerButtons.Weapon2,
                "BULLRUSH" => PlayerButtons.Bullrush,
                "GRENADE1" => PlayerButtons.Grenade1,
                "GRENADE2" => PlayerButtons.Grenade2,
                "ATTACK3" => PlayerButtons.Attack3,
                "SCOREBOARD" => PlayerButtons.Scoreboard,
                "INSPECT" => PlayerButtons.Inspect,
                _ => PlayerButtons.Use
            };
        }
    }
}