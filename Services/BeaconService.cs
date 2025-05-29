using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;

using ReviveSystem.Utils;

namespace ReviveSystem.Services
{
    public class BeaconService
    {
        private static readonly Vector VectorZero = new Vector(0, 0, 0);
        private static readonly QAngle RotationZero = new QAngle(0, 0, 0);

        public static void TeleportLaser(CBeam? laser, Vector start, Vector end)
        {
            if (laser == null || !laser.IsValid) return;

            laser.Teleport(start, RotationZero, VectorZero);
            laser.EndPos.X = end.X;
            laser.EndPos.Y = end.Y;
            laser.EndPos.Z = end.Z;

            Utilities.SetStateChanged(laser, "CBeam", "m_vecEndPos");
        }

        public static void DrawBeaconOnPlayer(CCSPlayerController? player, BasePlugin plugin)
        {
            if (player?.Pawn?.Value == null || player.PlayerPawn?.Value == null) return;

            var absOrigin = player.PlayerPawn?.Value?.AbsOrigin;
            if (absOrigin == null) return;

            Vector mid = new Vector(absOrigin.X, absOrigin.Y, absOrigin.Z);

            int lines = 20;
            int[] ent = new int[lines];
            CBeam[] beam_ent = new CBeam[lines];

            float step = (float)(2.0f * Math.PI) / lines;
            float radius = 20.0f;

            float angle_old = 0.0f;
            float angle_cur = step;

            for (int i = 0; i < lines; i++)
            {
                Vector start = BeaconUtils.AngleOnCircle(angle_old, radius, mid);
                Vector end = BeaconUtils.AngleOnCircle(angle_cur, radius, mid);

                var result = DrawLaserBetween(
                    start,
                    end,
                    player.TeamNum == 2 ? Color.Red : Color.Blue,
                    1.0f,
                    2.0f
                );

                if (result.Item2 == null) return;

                ent[i] = result.Item1;
                beam_ent[i] = result.Item2;
                angle_old = angle_cur;
                angle_cur += step;
            }

            plugin.AddTimer(0.1f, () =>
            {
                for (int i = 0; i < lines; i++)
                {
                    Vector start = BeaconUtils.AngleOnCircle(angle_old, radius, mid);
                    Vector end = BeaconUtils.AngleOnCircle(angle_cur, radius, mid);

                    TeleportLaser(beam_ent[i], start, end);

                    angle_old = angle_cur;
                    angle_cur += step;
                }
                radius += 10;
            }, TimerFlags.REPEAT);

            SoundService.PlaySoundOnPlayer(player, "sounds/tools/sfm/beep.vsnd_c");
        }

        private static (int, CBeam?) DrawLaserBetween(Vector startPos, Vector endPos, Color color, float life, float width)
        {
            if (startPos == null || endPos == null)
                return (-1, null);

            CBeam? beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (beam == null)
            {
                return (-1, null);
            }

            beam.Render = color;
            beam.Width = width;

            beam.Teleport(startPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            beam.EndPos.X = endPos.X;
            beam.EndPos.Y = endPos.Y;
            beam.EndPos.Z = endPos.Z;
            beam.DispatchSpawn();

            Server.NextFrame(() =>
            {
                Server.RunOnTick(Server.TickCount + (int)(life * 64), () =>
                {
                    if (beam?.IsValid == true) beam.Remove();
                });
            });

            return ((int)beam.Index, beam);
        }
    }
}