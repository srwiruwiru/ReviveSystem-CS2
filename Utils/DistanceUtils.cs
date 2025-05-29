using CounterStrikeSharp.API.Modules.Utils;

namespace ReviveSystem.Utils
{
    public static class DistanceUtils
    {
        public static float CalculateDistance(Vector? a, Vector? b)
        {
            if (a == null || b == null)
            {
                return float.MaxValue;
            }

            var distance = MathF.Sqrt(MathF.Pow(a.X - b.X, 2) + MathF.Pow(a.Y - b.Y, 2) + MathF.Pow(a.Z - b.Z, 2));
            return distance;
        }
    }
}