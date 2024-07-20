using CounterStrikeSharp.API.Modules.Cvars;

namespace SurfTimer.ST_Utils;

public static class ConVarHelper
{
    public static void RemoveCheatFlagFromConVar(string cvName)
    {
        ConVar? cv = ConVar.Find(cvName);
        if (cv == null || (cv.Flags & CounterStrikeSharp.API.ConVarFlags.FCVAR_CHEAT) == 0)
            return;

        cv.Flags &= ~CounterStrikeSharp.API.ConVarFlags.FCVAR_CHEAT;
    }
}