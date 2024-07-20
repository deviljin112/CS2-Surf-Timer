using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;

namespace SurfTimer.ST_Player.Replay;

public enum ReplayFrameSituation
{
        NONE,
        START_RUN,
        END_RUN,
        TOUCH_CHECKPOINT,
        START_STAGE,
        END_STAGE
}

[Serializable]
public class ReplayFrame 
{
        public Vector Pos { get; set; } = new Vector(0, 0, 0);
        public QAngle Ang { get; set; } = new QAngle(0, 0, 0);
        public uint Situation { get; set; } = (uint)ReplayFrameSituation.NONE;
        public ulong Button { get; set; }
        public uint Flags { get; set; }
        public MoveType_t MoveType { get; set; }
}
