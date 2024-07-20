using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer.ST_Player.Saveloc;

public class SavelocFrame
{
        public Vector Pos { get; set; } = new Vector(0, 0, 0);
        public QAngle Ang { get; set; } = new QAngle(0, 0, 0);
        public Vector Vel { get; set; } = new Vector(0, 0, 0);
        public int Tick { get; set; } = 0;
}
