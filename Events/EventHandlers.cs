using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;

using ReviveSystem.Utils;
using ReviveSystem.Managers;
using ReviveSystem.Models;

// NOTE: Same namespace as the plugin class because [GameEventHandler] methods only register on the plugin's own partial class
namespace ReviveSystem
{
    public partial class ReviveSystemBase
    {
        [GameEventHandler]
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var player = @event.Userid;

            if (player?.UserId == null || !player.IsValid || player.IsHLTV || player.Connected != PlayerConnectedState.Connected)
            {
                return HookResult.Continue;
            }

            PlayerManager.EnsurePlayerExists(player.UserId.Value, player.PlayerName);

            var playerPosition = player.PlayerPawn.Value?.AbsOrigin;
            var playerRotation = player.PlayerPawn.Value?.AbsRotation;

            PlayerManager.PlayersInfo[player.UserId.Value].DiePosition = new DiePosition(
                new Vector(
                    playerPosition?.X ?? 0,
                    playerPosition?.Y ?? 0,
                    playerPosition?.Z ?? 0
                ),
                new QAngle(
                    playerRotation?.X ?? 0,
                    playerRotation?.Y ?? 0,
                    playerRotation?.Z ?? 0
                )
            );

            Utils.Logger.LogDebug(LogCategory, $"Saved death position of {player.PlayerName} (user id {player.UserId.Value})");
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            TrackConnectedHumanPlayers();
            PlayerManager.ResetAllPlayersForNewRound();
            Utils.Logger.LogDebug(LogCategory, "Round started, revive state reset");
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;

            if (PlayerUtils.TryGetHumanUserId(player, out var userId))
            {
                PlayerManager.TrackNewPlayer(userId, player.PlayerName);
            }

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (@event.Userid?.UserId is int userId)
            {
                PlayerManager.StopTrackingPlayer(userId);
            }

            return HookResult.Continue;
        }

        private static void TrackConnectedHumanPlayers()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (PlayerUtils.TryGetHumanUserId(player, out var userId) && player.Connected == PlayerConnectedState.Connected)
                {
                    PlayerManager.EnsurePlayerExists(userId, player.PlayerName);
                }
            }
        }
    }
}