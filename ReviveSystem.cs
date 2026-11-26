using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using static CounterStrikeSharp.API.Core.Listeners;

using ReviveSystem.Utils;
using ReviveSystem.Config;
using ReviveSystem.Managers;
using ReviveSystem.Services;

namespace ReviveSystem
{
    [MinimumApiVersion(381)]
    public partial class ReviveSystemBase : BasePlugin, IPluginConfig<BaseConfigs>
    {
        public override string ModuleName => "ReviveSystem";
        public override string ModuleVersion => "0.1.5-beta";
        public override string ModuleAuthor => "luca.uy";
        public override string ModuleDescription => "Allows players to revive one of their teammates per round.";

        private readonly Dictionary<int, DateTime> LastBeaconTimes = new();
        private readonly Dictionary<int, DateTime> LastUpdateTimes = new();
        private ReviveManager? _reviveManager;

        public required BaseConfigs Config { get; set; }
        private PlayerButtons ReviveButtonFlag { get; set; } = PlayerButtons.Use;

        public override void Load(bool hotReload)
        {
            RegisterListener<OnTick>(OnTick);
        }

        public void OnConfigParsed(BaseConfigs config)
        {
            Config = config;
            ReviveButtonFlag = PlayerUtils.ParseReviveButton(config.ReviveButton);
            _reviveManager = new ReviveManager(config, this);
        }

        private void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || player.IsBot || !player.UserId.HasValue ||
                    !PlayerManager.PlayersInfo.ContainsKey(player.UserId.Value))
                {
                    continue;
                }

                if (!player.PawnIsAlive || !PlayerUtils.IsPlayerOnGround(player))
                {
                    continue;
                }

                var playerInfo = PlayerManager.PlayersInfo[player.UserId.Value];

                if (player.Buttons.HasFlag(ReviveButtonFlag))
                {
                    ProcessReviveAction(player, playerInfo);
                }
                else
                {
                    HandleReviveCancellation(player, playerInfo);
                }
            }
        }

        private void ProcessReviveAction(CCSPlayerController player, Models.PlayerInfo playerInfo)
        {
            var targetPlayerEntity = Utilities.GetPlayers().FirstOrDefault(p =>
                PlayerManager.PlayersInfo.Values.Any(t => t.UserId == p.UserId && t.DiePosition.HasValue));

            if (targetPlayerEntity == null || targetPlayerEntity.TeamNum != player.TeamNum)
            {
                HandleInvalidTarget(player, playerInfo, targetPlayerEntity);
                return;
            }

            if (playerInfo.ReviveCount >= Config.MaxRevivesPerRound)
            {
                HandleReviveLimitReached(player, playerInfo);
                return;
            }

            if (!playerInfo.UseStartTime.HasValue)
            {
                playerInfo.UseStartTime = DateTime.Now;
            }

            ProcessReviveProgress(player, playerInfo);
        }

        private void HandleInvalidTarget(CCSPlayerController player, Models.PlayerInfo playerInfo, CCSPlayerController? targetPlayerEntity)
        {
            var currentTime = DateTime.Now;
            var cooldownTime = TimeSpan.FromSeconds(1);

            if (!playerInfo.LastReviveLimitMessageTime.HasValue ||
                (currentTime - playerInfo.LastReviveLimitMessageTime.Value) >= cooldownTime)
            {
                if (targetPlayerEntity != null)
                {
                    player.PrintToChat($"{Localizer["prefix"]} {Localizer["DifferentTeam"]}");
                }
                playerInfo.LastReviveLimitMessageTime = currentTime;
            }
            playerInfo.UseStartTime = null;
        }

        private void HandleReviveLimitReached(CCSPlayerController player, Models.PlayerInfo playerInfo)
        {
            var currentTime = DateTime.Now;
            var cooldownTime = TimeSpan.FromSeconds(1);

            if (!playerInfo.LastReviveLimitMessageTime.HasValue ||
                (currentTime - playerInfo.LastReviveLimitMessageTime.Value) >= cooldownTime)
            {
                player.PrintToChat($"{Localizer["prefix"]} {Localizer["ReviveLimitReached"]}");
                playerInfo.LastReviveLimitMessageTime = currentTime;
            }
            playerInfo.UseStartTime = null;
        }

        private void ProcessReviveProgress(CCSPlayerController player, Models.PlayerInfo playerInfo)
        {
            foreach (var targetPlayer in PlayerManager.PlayersInfo.Values)
            {
                if (targetPlayer.DiePosition.HasValue &&
                    player.PlayerPawn?.Value != null &&
                    DistanceUtils.CalculateDistance(player.PlayerPawn.Value.AbsOrigin, targetPlayer.DiePosition.Value.Position) <= Config.ReviveRange)
                {
                    HandleReviveProgress(player, playerInfo, targetPlayer);
                    break;
                }
            }
        }

        private void HandleReviveProgress(CCSPlayerController player, Models.PlayerInfo playerInfo, Models.PlayerInfo targetPlayer)
        {
            int playerId = player.UserId!.Value;

            if (!LastBeaconTimes.TryGetValue(playerId, out var lastBeaconTime) ||
                (DateTime.Now - lastBeaconTime).TotalMilliseconds >= 1000)
            {
                BeaconService.DrawBeaconOnPlayer(player, this);
                LastBeaconTimes[playerId] = DateTime.Now;
            }

            var pressDuration = (DateTime.Now - playerInfo.UseStartTime!.Value).TotalSeconds;
            if (!LastUpdateTimes.TryGetValue(playerId, out var lastUpdateTime) ||
                (DateTime.Now - lastUpdateTime).TotalMilliseconds >= 40)
            {
                ShowReviveProgress(player, targetPlayer, pressDuration);
                LastUpdateTimes[playerId] = DateTime.Now;
            }

            if (pressDuration >= Config.ReviveTime && _reviveManager!.CanRevive(player, targetPlayer))
            {
                player.PrintToChat($"{Localizer["prefix"]} {string.Format(Localizer["ReviveComplete"], targetPlayer.Name)}");
                _reviveManager.RespawnPlayer(player, targetPlayer);
                playerInfo.UseStartTime = null;
                playerInfo.ReviveCount++;
            }
        }

        private void ShowReviveProgress(CCSPlayerController player, Models.PlayerInfo targetPlayer, double pressDuration)
        {
            var progressBarLength = 20;
            var filledLength = (int)(progressBarLength * (pressDuration / Config.ReviveTime));
            var emptyLength = progressBarLength - filledLength;
            var progressBar = new string('▌', filledLength) + new string('░', emptyLength);
            var percentage = (int)((pressDuration / Config.ReviveTime) * 100);

            player.PrintToCenterHtml($"{string.Format(Localizer["Reviving"], targetPlayer.Name)} 『{progressBar}』 {percentage}%");
        }

        private void HandleReviveCancellation(CCSPlayerController player, Models.PlayerInfo playerInfo)
        {
            if (playerInfo.UseStartTime.HasValue)
            {
                bool hasValidTarget = PlayerManager.PlayersInfo.Values.Any(targetPlayer =>
                    targetPlayer.DiePosition.HasValue &&
                    player.PlayerPawn?.Value != null &&
                    DistanceUtils.CalculateDistance(player.PlayerPawn.Value.AbsOrigin, targetPlayer.DiePosition.Value.Position) <= Config.ReviveRange
                );

                if (hasValidTarget)
                {
                    player.PrintToChat($"{Localizer["prefix"]} {Localizer["ReviveCancelled"]}");
                }

                playerInfo.UseStartTime = null;
            }
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<OnTick>(OnTick);
        }
    }
}