using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;

using ReviveSystem.Managers;
using ReviveSystem.Models;

namespace ReviveSystem.Events
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

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            PlayerManager.ResetAllPlayersForNewRound();
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
        {
            PlayerManager.ResetAllPlayersForNewRound();
            return HookResult.Continue;
        }
    }
}