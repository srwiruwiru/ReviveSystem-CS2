using CounterStrikeSharp.API.Modules.Utils;

namespace ReviveSystem.Utils
{
    public static class BeaconUtils
    {
        public static Vector AngleOnCircle(float angle, float radius, Vector mid)
        {
            return new Vector(
                (float)(mid.X + (radius * Math.Cos(angle))),
                (float)(mid.Y + (radius * Math.Sin(angle))),
                mid.Z + 6.0f
            );
        }
    }
}