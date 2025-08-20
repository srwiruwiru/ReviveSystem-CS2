using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace ReviveSystem
{
    [MinimumApiVersion(336)]
    public partial class ReviveSystemBase : BasePlugin, IPluginConfig<BaseConfigs>
    {
        public override string ModuleName => "ReviveSystem";
        public override string ModuleVersion => "0.1.6-beta";
        public override string ModuleAuthor => "luca.uy";
        public override string ModuleDescription => "Allows players to revive one of their teammates per round.";

        private static MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>? _cBasePlayerControllerSetPawnFunc;
        internal static readonly Dictionary<int, PlayerInfo> PlayersInfo = new();
        private readonly Dictionary<int, DateTime> LastBeaconTimes = new();
        private readonly Dictionary<int, DateTime> LastUpdateTimes = new();

        public required BaseConfigs Config { get; set; }
        private PlayerButtons ReviveButtonFlag { get; set; } = PlayerButtons.Use;

        public override void Load(bool hotReload)
        {

            _cBasePlayerControllerSetPawnFunc = new MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>(
                GameData.GetSignature("CBasePlayerController_SetPawn")
            );

            RegisterListener<OnTick>(OnTick);
        }

        public void OnConfigParsed(BaseConfigs config)
        {
            Config = config;
            ReviveButtonFlag = ParseReviveButton(config.ReviveButton);
        }

        private void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || player.IsBot || !player.UserId.HasValue || !PlayersInfo.ContainsKey(player.UserId.Value))
                {
                    continue;
                }

                if (!player.PawnIsAlive)
                {
                    continue;
                }

                if (!IsPlayerOnGround(player))
                {
                    continue;
                }

                var playerInfo = PlayersInfo[player.UserId.Value];

                if (player.Buttons.HasFlag(ReviveButtonFlag))
                {

                    var targetPlayerEntity = Utilities.GetPlayers().FirstOrDefault(p => PlayersInfo.Values.Any(t => t.UserId == p.UserId && t.DiePosition.HasValue));
                    if (targetPlayerEntity == null || targetPlayerEntity.TeamNum != player.TeamNum)
                    {
                        var currentTime = DateTime.Now;
                        var cooldownTime = TimeSpan.FromSeconds(1);

                        if (!playerInfo.LastReviveLimitMessageTime.HasValue || (currentTime - playerInfo.LastReviveLimitMessageTime.Value) >= cooldownTime)
                        {
                            if (targetPlayerEntity != null)
                            {
                                player.PrintToChat($"{Localizer["prefix"]} {Localizer["DifferentTeam"]}");
                            }

                            playerInfo.LastReviveLimitMessageTime = currentTime;
                        }

                        playerInfo.UseStartTime = null;
                        return;
                    }

                    if (playerInfo.ReviveCount >= Config.MaxRevivesPerRound)
                    {
                        var currentTime = DateTime.Now;
                        var cooldownTime = TimeSpan.FromSeconds(1);
                        if (!playerInfo.LastReviveLimitMessageTime.HasValue || (currentTime - playerInfo.LastReviveLimitMessageTime.Value) >= cooldownTime)
                        {
                            player.PrintToChat($"{Localizer["prefix"]} {Localizer["ReviveLimitReached"]}");
                            playerInfo.LastReviveLimitMessageTime = currentTime;
                        }
                        playerInfo.UseStartTime = null;
                        continue;
                    }

                    if (!playerInfo.UseStartTime.HasValue)
                    {
                        playerInfo.UseStartTime = DateTime.Now;
                    }

                    foreach (var targetPlayer in PlayersInfo.Values)
                    {
                        if (targetPlayer.DiePosition.HasValue &&
                            player.PlayerPawn?.Value != null &&
                            CalculateDistance(player.PlayerPawn.Value.AbsOrigin, targetPlayer.DiePosition.Value.Position) <= Config.ReviveRange)
                        {
                            int playerId = player.UserId.Value;

                            if (!LastBeaconTimes.ContainsKey(playerId) || (DateTime.Now - LastBeaconTimes[playerId]).TotalMilliseconds >= 1000)
                            {
                                DrawBeaconOnPlayer(player);
                                LastBeaconTimes[playerId] = DateTime.Now;
                            }

                            var pressDuration = (DateTime.Now - playerInfo.UseStartTime.Value).TotalSeconds;

                            if (!LastUpdateTimes.ContainsKey(playerId) || (DateTime.Now - LastUpdateTimes[playerId]).TotalMilliseconds >= 40)
                            {
                                var progressBarLength = 20;
                                var filledLength = (int)(progressBarLength * (pressDuration / Config.ReviveTime));
                                var emptyLength = progressBarLength - filledLength;
                                var progressBar = new string('▌', filledLength) + new string('░', emptyLength);
                                var percentage = (int)((pressDuration / Config.ReviveTime) * 100);

                                // player.PrintToCenterHtml($"{Localizer["prefix"]} {string.Format(Localizer["Reviving"], targetPlayer.Name)}: 『{progressBar}』 {percentage}%");
                                player.PrintToCenterHtml($"{string.Format(Localizer["Reviving"], targetPlayer.Name)} 『{progressBar}』 {percentage}%");
                                LastUpdateTimes[playerId] = DateTime.Now;
                            }

                            if (pressDuration >= Config.ReviveTime && CanRevive(player, targetPlayer))
                            {
                                player.PrintToChat($"{Localizer["prefix"]} {string.Format(Localizer["ReviveComplete"], targetPlayer.Name)}");
                                RespawnPlayer(player, targetPlayer);
                                playerInfo.UseStartTime = null;

                                playerInfo.ReviveCount++;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (playerInfo.UseStartTime.HasValue)
                    {
                        bool hasValidTarget = PlayersInfo.Values.Any(targetPlayer =>
                            targetPlayer.DiePosition.HasValue &&
                            player.PlayerPawn?.Value != null &&
                            CalculateDistance(player.PlayerPawn.Value.AbsOrigin, targetPlayer.DiePosition.Value.Position) <= Config.ReviveRange
                        );

                        if (hasValidTarget)
                        {
                            player.PrintToChat($"{Localizer["prefix"]} {Localizer["ReviveCancelled"]}");
                        }

                        playerInfo.UseStartTime = null;
                    }

                }
            }
        }

        private bool IsPlayerOnGround(CCSPlayerController player)
        {
            if (player.PlayerPawn?.Value == null)
                return false;

            var playerFlags = player.PlayerPawn.Value.Flags;
            return (playerFlags & (uint)PlayerFlags.FL_ONGROUND) == (uint)PlayerFlags.FL_ONGROUND;
        }

        private float CalculateDistance(Vector? a, Vector? b)
        {
            if (a == null || b == null)
            {
                return float.MaxValue;
            }

            var distance = MathF.Sqrt(MathF.Pow(a.X - b.X, 2) + MathF.Pow(a.Y - b.Y, 2) + MathF.Pow(a.Z - b.Z, 2));
            return distance;
        }

        private bool CanRevive(CCSPlayerController player, PlayerInfo targetPlayer)
        {
            if (!string.IsNullOrEmpty(Config.CanReviveOthersFlag) &&
                !AdminManager.PlayerHasPermissions(player, Config.CanReviveOthersFlag))
            {
                player.PrintToChat($"{Localizer["prefix"]} {Localizer["NoPermissions"]}");
                return false;
            }

            if (!string.IsNullOrEmpty(Config.CanBeRevivedFlag))
            {
                var targetPlayerController = Utilities.GetPlayers().FirstOrDefault(p => p.UserId == targetPlayer.UserId);
                if (targetPlayerController == null || !AdminManager.PlayerHasPermissions(targetPlayerController, Config.CanBeRevivedFlag))
                {
                    player.PrintToChat($"{Localizer["prefix"]} {string.Format(Localizer["TargetNoPermissions"], targetPlayer.Name)}");
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

        private PlayerButtons ParseReviveButton(string buttonName)
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

        public struct DiePosition
        {
            public Vector Position { get; set; }
            public QAngle Angle { get; set; }
            public DiePosition(Vector position, QAngle angle)
            {
                Position = position;
                Angle = angle;
            }
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<OnTick>(OnTick);
        }
    }
}
