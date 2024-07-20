using CounterStrikeSharp.API.Core;
using SurfTimer.ST_Map;
using SurfTimer.ST_Player.Replay;
using SurfTimer.ST_Player.Saveloc;
using SurfTimer.ST_Player.Stats;

namespace SurfTimer.ST_Player;

public class Player 
{
    // CCS requirements
    public CCSPlayerController? Controller {get;}
    public CCSPlayer_MovementServices MovementServices {get;} // Can be used later for any movement modification (eg: styles)

    // Timer-related properties
    public PlayerTimer Timer {get; set;}
    public PlayerStats Stats {get; set;}
    public PlayerHUD HUD {get; set;}
    public ReplayRecorder ReplayRecorder { get; set; }
    public List<SavelocFrame> SavedLocations { get; set; }
    public int CurrentSavedLocation { get; set; }

    // Player information
    public PlayerProfile Profile {get; set;}

    // Map information
    public Map CurrMap;

    // Constructor
    public Player(CCSPlayerController? controller, CCSPlayer_MovementServices movementServices, PlayerProfile profile, Map? currMap)
    {
        Controller = controller;
        MovementServices = movementServices;

        Profile = profile;

        Timer = new PlayerTimer();
        Stats = new PlayerStats();
        ReplayRecorder = new ReplayRecorder();
        SavedLocations = new List<SavelocFrame>();
        CurrentSavedLocation = 0;

        HUD = new PlayerHUD(this);
        CurrMap = currMap!;
    }

    /// <summary>
    /// Checks if current player is spectating player 'p'
    /// </summary>
    public bool IsSpectating(CCSPlayerController? p)
    {
        if(p == null || Controller == null || Controller.Team != CounterStrikeSharp.API.Modules.Utils.CsTeam.Spectator)
            return false;

        return p.Pawn.SerialNum == Controller.ObserverPawn.Value!.ObserverServices!.ObserverTarget.SerialNum;
    }
}
