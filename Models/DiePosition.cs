using CounterStrikeSharp.API.Modules.Utils;

namespace ReviveSystem.Models
{
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
}