using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SurfTimer.ST_Game;
using SurfTimer.ST_Player;
using SurfTimer.ST_Player.Replay;
using SurfTimer.ST_Player.Saveloc;

namespace SurfTimer.ST_Commands;

public class PlayerCommands
{
    private readonly ILogger<SurfTimer> _logger;
    private readonly SurfTimer _plugin;
    private readonly GameManager _gameManager;

    public PlayerCommands(ILogger<SurfTimer> logger, SurfTimer plugin, GameManager gameManager)
    {
        _logger = logger;
        _plugin = plugin;
        _gameManager = gameManager;
    }

    public void Init()
    {
        _plugin.AddCommand("css_r", "Reset back to the start of the map.", PlayerReset);
        _plugin.AddCommand("css_rs", "Reset back to the start of the stage or bonus you're in.", PlayerResetStage);
        _plugin.AddCommand("css_s", "Teleport to a stage", PlayerGoToStage);
        _plugin.AddCommand("css_spec", "Moves a player automaticlly into spectator mode", MovePlayerToSpectator);
        _plugin.AddCommand("css_replaybotpause", "Pause the replay bot playback", PauseReplay);
        _plugin.AddCommand("css_rbpause", "Pause the replay bot playback", PauseReplay);
        _plugin.AddCommand("css_replaybotflip", "Flips the replay bot between Forward/Backward playback", ReverseReplay);
        _plugin.AddCommand("css_rbflip", "Flips the replay bot between Forward/Backward playback", ReverseReplay);
        _plugin.AddCommand("css_pbreplay", "Allows for replay of player's PB", PbReplay);
        _plugin.AddCommand("css_saveloc", "Save current player location to be practiced", SavePlayerLocation);
        _plugin.AddCommand("css_tele", "Teleport player to current saved location", TeleportPlayerLocation);
        _plugin.AddCommand("css_teleprev", "Teleport player to previous saved location", TeleportPlayerLocationPrev);
        _plugin.AddCommand("css_telenext", "Teleport player to next saved location", TeleportPlayerLocationNext);
    }
    
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void PlayerReset(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        // To-do: players[userid].Timer.Reset() -> teleport player
        _gameManager.PlayerList[player.UserId ?? 0].Timer.Reset();
        if (_gameManager.CurrentMap.StartZone != new Vector(0, 0, 0))
            Server.NextFrame(() =>
                player.PlayerPawn.Value!.Teleport(_gameManager.CurrentMap.StartZone, new QAngle(0, 0, 0), new Vector(0, 0, 0)));
        return;
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void PlayerResetStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        // To-do: players[userid].Timer.Reset() -> teleport player
        Player SurfPlayer = _gameManager.PlayerList[player.UserId ?? 0];
        if (SurfPlayer.Timer.Stage != 0 && _gameManager.CurrentMap.StageStartZone[SurfPlayer.Timer.Stage] != new Vector(0, 0, 0))
            Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(_gameManager.CurrentMap.StageStartZone[SurfPlayer.Timer.Stage],
                _gameManager.CurrentMap.StageStartZoneAngles[SurfPlayer.Timer.Stage], new Vector(0, 0, 0)));
        else // Reset back to map start
            Server.NextFrame(() =>
                player.PlayerPawn.Value!.Teleport(_gameManager.CurrentMap.StartZone, new QAngle(0, 0, 0), new Vector(0, 0, 0)));
        return;
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void PlayerGoToStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        int stage = Int32.Parse(command.ArgByIndex(1)) - 1;
        if (stage > _gameManager.CurrentMap.Stages - 1 && _gameManager.CurrentMap.Stages > 0)
            stage = _gameManager.CurrentMap.Stages - 1;

        // Must be 1 argument
        if (command.ArgCount < 2 || stage < 0)
        {
#if DEBUG
            player.PrintToChat(
                $"CS2 Surf DEBUG >> css_s >> Arg#: {command.ArgCount} >> Args: {Int32.Parse(command.ArgByIndex(1))}");
#endif

            player.PrintToChat(
                $"{_gameManager.PluginPrefix} {ChatColors.Red}Invalid arguments. Usage: {ChatColors.Green}!s <stage>");
            return;
        }
        else if (_gameManager.CurrentMap.Stages <= 0)
        {
            player.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}This map has no stages.");
            return;
        }

        if (_gameManager.CurrentMap.StageStartZone[stage] != new Vector(0, 0, 0))
        {
            if (stage == 0)
                Server.NextFrame(() =>
                    player.PlayerPawn.Value!.Teleport(_gameManager.CurrentMap.StartZone, _gameManager.CurrentMap.StartZoneAngles,
                        new Vector(0, 0, 0)));
            else
                Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(_gameManager.CurrentMap.StageStartZone[stage],
                    _gameManager.CurrentMap.StageStartZoneAngles[stage], new Vector(0, 0, 0)));

            _gameManager.PlayerList[player.UserId ?? 0].Timer.Reset();
            _gameManager.PlayerList[player.UserId ?? 0].Timer.IsStageMode = true;

            // To-do: If you run this while you're in the start zone, endtouch for the start zone runs after you've teleported
            //        causing the timer to start. This needs to be fixed.
        }

        else
            player.PrintToChat(
                $"{_gameManager.PluginPrefix} {ChatColors.Red}Invalid stage provided. Usage: {ChatColors.Green}!s <stage>");
    }

    private void MovePlayerToSpectator(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team == CsTeam.Spectator)
            return;

        player.ChangeTeam(CsTeam.Spectator);
    }

    /*
    #########################
        Reaplay Commands
    #########################
    */
    private void PauseReplay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team != CsTeam.Spectator)
            return;

        foreach (ReplayPlayer rb in _gameManager.CurrentMap.ReplayBots)
        {
            if (!rb.IsPlayable || !rb.IsPlaying || !_gameManager.PlayerList[player.UserId ?? 0].IsSpectating(rb.Controller!))
                continue;

            rb.Pause();
        }
    }

    private void ReverseReplay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team != CsTeam.Spectator)
            return;

        foreach (ReplayPlayer rb in _gameManager.CurrentMap.ReplayBots)
        {
            if (!rb.IsPlayable || !rb.IsPlaying || !_gameManager.PlayerList[player.UserId ?? 0].IsSpectating(rb.Controller!))
                continue;

            rb.FrameTickIncrement *= -1;
        }
    }

    private void PbReplay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        int maptime_id = _gameManager.PlayerList[player!.UserId ?? 0].Stats.PB[_gameManager.PlayerList[player.UserId ?? 0].Timer.Style].ID;
        if (command.ArgCount > 1)
        {
            try
            {
                maptime_id = int.Parse(command.ArgByIndex(1));
            }
            catch
            {
            }
        }

        if (maptime_id == -1 || !_gameManager.CurrentMap.ConnectedMapTimes.Contains(maptime_id))
        {
            player.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}No time was found");
            return;
        }
        
        Console.WriteLine($"[CS2 SURF] Replay Bot: {_gameManager.CurrentMap.ReplayBots}");
        Console.WriteLine($"[CS2 SURF] Replay Bot Count: {_gameManager.CurrentMap.ReplayBots.Count}");
        
        for (int i = 0; i < _gameManager.CurrentMap.ReplayBots.Count; i++)
        {
            if (_gameManager.CurrentMap.ReplayBots[i].Stat_MapTimeID == maptime_id)
            {
                player.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}A bot of this run already playing");
                return;
            }
        }

        _gameManager.CurrentMap.ReplayBots = _gameManager.CurrentMap.ReplayBots.Prepend(new ReplayPlayer()
        {
            Stat_MapTimeID = maptime_id,
            Stat_Prefix = "PB"
        }).ToList();

        Server.NextFrame(() => { Server.ExecuteCommand($"bot_quota {_gameManager.CurrentMap.ReplayBots.Count}"); });
    }

    /*
########################
    Saveloc Commands
########################
*/
    private void SavePlayerLocation(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PawnIsAlive || !_gameManager.PlayerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = _gameManager.PlayerList[player.UserId ?? 0];
        if (!p.Timer.IsRunning)
        {
            p.Controller.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}Cannot save location while not in run");
            return;
        }

        var player_pos = p.Controller.Pawn.Value!.AbsOrigin!;
        var player_angle = p.Controller.PlayerPawn.Value!.EyeAngles;
        var player_velocity = p.Controller.PlayerPawn.Value!.AbsVelocity;

        p.SavedLocations.Add(new SavelocFrame
        {
            Pos = new Vector(player_pos.X, player_pos.Y, player_pos.Z),
            Ang = new QAngle(player_angle.X, player_angle.Y, player_angle.Z),
            Vel = new Vector(player_velocity.X, player_velocity.Y, player_velocity.Z),
            Tick = p.Timer.Ticks
        });
        p.CurrentSavedLocation = p.SavedLocations.Count - 1;

        p.Controller.PrintToChat(
            $"{_gameManager.PluginPrefix} {ChatColors.Green}Saved location! {ChatColors.Default} use !tele {p.SavedLocations.Count - 1} to teleport to this location");
    }

    private void TeleportPlayerLocation(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PawnIsAlive || !_gameManager.PlayerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = _gameManager.PlayerList[player.UserId ?? 0];

        if (p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}No saved locations");
            return;
        }

        if (!p.Timer.IsRunning)
            p.Timer.Start();

        if (!p.Timer.IsPracticeMode)
        {
            p.Controller.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}Timer now on practice");
            p.Timer.IsPracticeMode = true;
        }

        if (command.ArgCount > 1)
            try
            {
                int tele_n = int.Parse(command.ArgByIndex(1));
                if (tele_n < p.SavedLocations.Count)
                    p.CurrentSavedLocation = tele_n;
            }
            catch
            {
            }

        SavelocFrame location = p.SavedLocations[p.CurrentSavedLocation];
        Server.NextFrame(() =>
        {
            p.Controller.PlayerPawn.Value!.Teleport(location.Pos, location.Ang, location.Vel);
            p.Timer.Ticks = location.Tick;
        });

        p.Controller.PrintToChat($"{_gameManager.PluginPrefix} Teleported #{p.CurrentSavedLocation}");
    }

    private void TeleportPlayerLocationPrev(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PawnIsAlive || !_gameManager.PlayerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = _gameManager.PlayerList[player.UserId ?? 0];

        if (p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}No saved locations");
            return;
        }

        if (p.CurrentSavedLocation == 0)
        {
            p.Controller.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}Already at first location");
        }
        else
        {
            p.CurrentSavedLocation--;
        }

        TeleportPlayerLocation(player, command);

        p.Controller.PrintToChat($"{_gameManager.PluginPrefix} Teleported #{p.CurrentSavedLocation}");
    }

    private void TeleportPlayerLocationNext(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PawnIsAlive || !_gameManager.PlayerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = _gameManager.PlayerList[player.UserId ?? 0];

        if (p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}No saved locations");
            return;
        }

        if (p.CurrentSavedLocation == p.SavedLocations.Count - 1)
        {
            p.Controller.PrintToChat($"{_gameManager.PluginPrefix} {ChatColors.Red}Already at last location");
        }
        else
        {
            p.CurrentSavedLocation++;
        }

        TeleportPlayerLocation(player, command);

        p.Controller.PrintToChat($"{_gameManager.PluginPrefix} Teleported #{p.CurrentSavedLocation}");
    }
}