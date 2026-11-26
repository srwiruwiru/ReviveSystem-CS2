using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

using ReviveSystem.Configs;
using ReviveSystem.Models;

namespace ReviveSystem.Managers
{
    public class ReviveManager
    {
        private static MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>? _cBasePlayerControllerSetPawnFunc;
        private readonly PermissionsConfig _config;
        private readonly BasePlugin _plugin;

        public ReviveManager(PermissionsConfig config, BasePlugin plugin)
        {
            _config = config;
            _plugin = plugin;

            _cBasePlayerControllerSetPawnFunc = new MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>(
                GameData.GetSignature("CBasePlayerController_SetPawn")
            );
        }

        public bool CanRevive(CCSPlayerController player, PlayerInfo targetPlayer)
        {
            if (!string.IsNullOrEmpty(_config.CanReviveOthersFlag) &&
                !AdminManager.PlayerHasPermissions(player, _config.CanReviveOthersFlag))
            {
                player.PrintToChat($"{_plugin.Localizer["prefix"]} {_plugin.Localizer["NoPermissions"]}");
                return false;
            }

            if (!string.IsNullOrEmpty(_config.CanBeRevivedFlag))
            {
                var targetPlayerController = Utilities.GetPlayers().FirstOrDefault(p => p.UserId == targetPlayer.UserId);
                if (targetPlayerController == null || !AdminManager.PlayerHasPermissions(targetPlayerController, _config.CanBeRevivedFlag))
                {
                    player.PrintToChat($"{_plugin.Localizer["prefix"]} {string.Format(_plugin.Localizer["TargetNoPermissions"], targetPlayer.Name)}");
                    return false;
                }
            }

            if (!player.PawnIsAlive)
            {
                return false;
            }

            return true;
        }

        public void RespawnPlayer(CCSPlayerController caller, PlayerInfo targetPlayerInfo)
        {
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.UserId == targetPlayerInfo.UserId);
            if (player == null || player.PlayerPawn?.Value == null || !player.PlayerPawn.IsValid)
            {
                return;
            }

            var playerPawn = player.PlayerPawn.Value;
            _cBasePlayerControllerSetPawnFunc?.Invoke(player, playerPawn, true, false);

            VirtualFunction.CreateVoid<CCSPlayerController>(player.Handle, GameData.GetOffset("CCSPlayerController_Respawn"))(player);

            if (targetPlayerInfo.DiePosition.HasValue)
            {
                var diePosition = targetPlayerInfo.DiePosition.Value;
                playerPawn.Teleport(diePosition.Position, diePosition.Angle);
            }

            targetPlayerInfo.DiePosition = null;
        }
    }
}