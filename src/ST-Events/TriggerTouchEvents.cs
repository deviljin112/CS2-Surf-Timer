using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SurfTimer.ST_DB;
using SurfTimer.ST_Game;
using SurfTimer.ST_Player;
using SurfTimer.ST_Player.Replay;
using SurfTimer.ST_Player.Stats;

namespace SurfTimer.ST_Events;

public class TriggerTouchEvents
{
    private readonly ILogger<SurfTimer> _logger;
    private readonly SurfTimer _plugin;
    private readonly GameManager _gameManager;
    private readonly TimerDatabase _database;

    public TriggerTouchEvents(ILogger<SurfTimer> logger, SurfTimer plugin, GameManager gameManager, TimerDatabase database)
    {
        _logger = logger;
        _plugin = plugin;
        _gameManager = gameManager;
        _database = database;
    }

    public void Init()
    {
        VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(OnTriggerStartTouch, HookMode.Post);
        VirtualFunctions.CBaseTrigger_EndTouchFunc.Hook(OnTriggerEndTouch, HookMode.Post);
    }

    private HookResult OnTriggerEndTouch(DynamicHook handler)
    {
        CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
        CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
        CCSPlayerController client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        if (!client.IsValid || client.UserId == -1 || !client.PawnIsAlive ||
            !_gameManager.PlayerList
                .ContainsKey((int)client
                        .UserId
                    !)) // `client.IsBot` throws error in server console when going to spectator? + !playerList.ContainsKey((int)client.UserId!) make sure to not check for user_id that doesnt exists
        {
            return HookResult.Continue;
        }


        // Implement Trigger End Touch Here
        Player player = _gameManager.PlayerList[client.UserId ?? 0];
#if DEBUG
        player.Controller.PrintToChat(
            $"CS2 Surf DEBUG >> CBaseTrigger_EndTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
#endif

        if (trigger.Entity!.Name != null)
        {
            // Get velocities for DB queries
            // Get the velocity of the player - we will be using this values to compare and write to DB
            float velocity_x = player.Controller.PlayerPawn.Value!.AbsVelocity.X;
            float velocity_y = player.Controller.PlayerPawn.Value!.AbsVelocity.Y;
            float velocity_z = player.Controller.PlayerPawn.Value!.AbsVelocity.Z;
            float velocity =
                (float)Math.Sqrt(velocity_x * velocity_x + velocity_y * velocity_y + velocity_z + velocity_z);

            // Map start zones -- hook into map_start, (s)tage1_start
            if (trigger.Entity.Name.Contains("map_start") ||
                trigger.Entity.Name.Contains("s1_start") ||
                trigger.Entity.Name.Contains("stage1_start"))
            {
                // Replay
                if (player.ReplayRecorder.IsRecording)
                {
                    // Saveing 2 seconds before leaving the start zone
                    player.ReplayRecorder.Frames.RemoveRange(0,
                        Math.Max(0,
                            player.ReplayRecorder.Frames.Count -
                            (64 * 2))); // Todo make a plugin convar for the time saved before start of run 
                }

                // MAP START ZONE
                player.Timer.Start();
                player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.START_RUN;

                /* Revisit
                // Wonky Prespeed check
                // To-do: make the teleportation a bit more elegant (method in a class or something)
                if (velocity > 666.0)
                {
                    player.Controller.PrintToChat(
                        $"{PluginPrefix} {ChatColors.Red}You are going too fast! ({velocity.ToString("0")} u/s)");
                    player.Timer.Reset();
                    if (CurrentMap.StartZone != new Vector(0,0,0))
                        Server.NextFrame(() => player.Controller.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, new QAngle(0,0,0), new Vector(0,0,0)));
                }
                */

                // Prespeed display
                player.Controller.PrintToCenter($"Prespeed: {velocity.ToString("0")} u/s");
                player.Stats.ThisRun.StartVelX = velocity_x; // Start pre speed for the run
                player.Stats.ThisRun.StartVelY = velocity_y; // Start pre speed for the run
                player.Stats.ThisRun.StartVelZ = velocity_z; // Start pre speed for the run

#if DEBUG
                player.Controller.PrintToChat(
                    $"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
#endif
            }

            // Stage start zones -- hook into (s)tage#_start
            else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success)
            {
#if DEBUG
                player.Controller.PrintToChat(
                    $"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                Console.WriteLine(
                    $"===================== player.Timer.Checkpoint {player.Timer.Checkpoint} - player.Stats.ThisRun.Checkpoint.Count {player.Stats.ThisRun.Checkpoint.Count}");
#endif

                // This will populate the End velocities for the given Checkpoint zone (Stage = Checkpoint when in a Map Run)
                if (player.Timer.Checkpoint != 0 &&
                    player.Timer.Checkpoint <= player.Stats.ThisRun.Checkpoint.Count)
                {
                    var currentCheckpoint = player.Stats.ThisRun.Checkpoint[player.Timer.Checkpoint];
#if DEBUG
                    Console.WriteLine(
                        $"currentCheckpoint.EndVelX {currentCheckpoint.EndVelX} - velocity_x {velocity_x}");
                    Console.WriteLine(
                        $"currentCheckpoint.EndVelY {currentCheckpoint.EndVelY} - velocity_y {velocity_y}");
                    Console.WriteLine(
                        $"currentCheckpoint.EndVelZ {currentCheckpoint.EndVelZ} - velocity_z {velocity_z}");
                    Console.WriteLine($"currentCheckpoint.Attempts {currentCheckpoint.Attempts}");
#endif

                    // Update the values
                    currentCheckpoint.EndVelX = velocity_x;
                    currentCheckpoint.EndVelY = velocity_y;
                    currentCheckpoint.EndVelZ = velocity_z;
                    currentCheckpoint.EndTouch = player.Timer.Ticks; // To-do: what type of value we store in DB ?
                    currentCheckpoint.Attempts += 1;
                    // Assign the updated currentCheckpoint back to the list as `currentCheckpoint` is supposedly a copy of the original object
                    player.Stats.ThisRun.Checkpoint[player.Timer.Checkpoint] = currentCheckpoint;

                    // Show Prespeed for stages - will be enabled/disabled by the user?
                    player.Controller.PrintToCenter(
                        $"Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} - Prespeed: {velocity.ToString("0")} u/s");
                }
                else
                {
                    // Handle the case where the index is out of bounds
                }
            }

            // Checkpoint zones -- hook into "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$" map_c(heck)p(oint) 
            else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success)
            {
#if DEBUG
                player.Controller.PrintToChat(
                    $"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.LightRed}EndTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
                _logger.LogDebug(
                    $"===================== player.Timer.Checkpoint {player.Timer.Checkpoint} - player.Stats.ThisRun.Checkpoint.Count {player.Stats.ThisRun.Checkpoint.Count}");
#endif

                // This will populate the End velocities for the given Checkpoint zone (Stage = Checkpoint when in a Map Run)
                if (player.Timer.Checkpoint != 0 &&
                    player.Timer.Checkpoint <= player.Stats.ThisRun.Checkpoint.Count)
                {
                    var currentCheckpoint = player.Stats.ThisRun.Checkpoint[player.Timer.Checkpoint];
#if DEBUG
                    _logger.LogDebug(
                        $"currentCheckpoint.EndVelX {currentCheckpoint.EndVelX} - velocity_x {velocity_x}");
                    _logger.LogDebug(
                        $"currentCheckpoint.EndVelY {currentCheckpoint.EndVelY} - velocity_y {velocity_y}");
                    _logger.LogDebug(
                        $"currentCheckpoint.EndVelZ {currentCheckpoint.EndVelZ} - velocity_z {velocity_z}");
#endif

                    // Update the values
                    currentCheckpoint.EndVelX = velocity_x;
                    currentCheckpoint.EndVelY = velocity_y;
                    currentCheckpoint.EndVelZ = velocity_z;
                    currentCheckpoint.EndTouch = player.Timer.Ticks; // To-do: what type of value we store in DB ?
                    currentCheckpoint.Attempts += 1;
                    // Assign the updated currentCheckpoint back to the list as `currentCheckpoint` is supposedly a copy of the original object
                    player.Stats.ThisRun.Checkpoint[player.Timer.Checkpoint] = currentCheckpoint;

                    // Show Prespeed for stages - will be enabled/disabled by the user?
                    player.Controller.PrintToCenter(
                        $"Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} - Prespeed: {velocity.ToString("0")} u/s");
                }
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnTriggerStartTouch(DynamicHook handler)
    {
        CBaseTrigger trigger = handler.GetParam<CBaseTrigger>(0);
        CBaseEntity entity = handler.GetParam<CBaseEntity>(1);
        CCSPlayerController client = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        if (!client.IsValid || !client.PawnIsAlive ||
            !_gameManager.PlayerList.ContainsKey((int)client
                    .UserId
                !)) // !playerList.ContainsKey((int)client.UserId!) make sure to not check for user_id that doesnt exists
        {
            return HookResult.Continue;
        }
        else
        {
            // To-do: Sometimes this triggers before `OnPlayerConnect` and `playerList` does not contain the player how is this possible :thonk:
            if (!_gameManager.PlayerList.ContainsKey(client.UserId ?? 0))
            {
                Console.WriteLine(
                    $"CS2 Surf ERROR >> OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {client.PlayerName} ({client.UserId})");
                throw new Exception(
                    $"CS2 Surf ERROR >> OnTriggerStartTouch -> Init -> Player playerList does NOT contain client.UserId, this shouldn't happen. Player: {client.PlayerName} ({client.UserId})");
                // return HookResult.Continue;
            }

            // Implement Trigger Start Touch Here
            Player player = _gameManager.PlayerList[client.UserId ?? 0];
#if DEBUG
            player.Controller.PrintToChat(
                $"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> {trigger.DesignerName} -> {trigger.Entity!.Name}");
#endif

            if (trigger.Entity!.Name != null)
            {
                // Get velocities for DB queries
                // Get the velocity of the player - we will be using this values to compare and write to DB
                float velocity_x = player.Controller.PlayerPawn.Value!.AbsVelocity.X;
                float velocity_y = player.Controller.PlayerPawn.Value!.AbsVelocity.Y;
                float velocity_z = player.Controller.PlayerPawn.Value!.AbsVelocity.Z;
                float velocity =
                    (float)Math.Sqrt(velocity_x * velocity_x + velocity_y * velocity_y + velocity_z + velocity_z);
                int style = player.Timer.Style;

                // Map end zones -- hook into map_end
                if (trigger.Entity.Name == "map_end")
                {
                    player.Controller.PrintToCenter($"Map End");
                    // MAP END ZONE
                    if (player.Timer.IsRunning)
                    {
                        player.Timer.Stop();
                        player.ReplayRecorder.CurrentSituation = ReplayFrameSituation.END_RUN;

                        player.Stats.ThisRun.Ticks = player.Timer.Ticks; // End time for the run
                        player.Stats.ThisRun.EndVelX = velocity_x; // End pre speed for the run
                        player.Stats.ThisRun.EndVelY = velocity_y; // End pre speed for the run
                        player.Stats.ThisRun.EndVelZ = velocity_z; // End pre speed for the run

                        string PracticeString = "";
                        if (player.Timer.IsPracticeMode)
                            PracticeString = $"({ChatColors.Grey}Practice{ChatColors.Default}) ";

                        // To-do: make Style (currently 0) be dynamic
                        if (player.Stats.PB[style].Ticks <= 0) // Player first ever PersonalBest for the map
                        {
                            Server.PrintToChatAll(
                                $"{_gameManager.PluginPrefix} {PracticeString}{player.Controller.PlayerName} finished the map in {ChatColors.Gold}{PlayerHUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} ({player.Timer.Ticks})!");
                        }
                        else if (player.Timer.Ticks <
                                 player.Stats.PB[style].Ticks) // Player beating their existing PersonalBest for the map
                        {
                            Server.PrintToChatAll(
                                $"{_gameManager.PluginPrefix} {PracticeString}{ChatColors.Lime}{player.Profile.Name}{ChatColors.Default} beat their PB in {ChatColors.Gold}{PlayerHUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default} (Old: {ChatColors.BlueGrey}{PlayerHUD.FormatTime(player.Stats.PB[style].Ticks)}{ChatColors.Default})!");
                        }
                        else // Player did not beat their existing PersonalBest for the map
                        {
                            player.Controller.PrintToChat(
                                $"{_gameManager.PluginPrefix} {PracticeString}You finished the map in {ChatColors.Yellow}{PlayerHUD.FormatTime(player.Timer.Ticks)}{ChatColors.Default}!");
                            return HookResult.Continue; // Exit here so we don't write to DB
                        }

                        if (_database == null)
                            throw new Exception(
                                "CS2 Surf ERROR >> OnTriggerStartTouch (Map end zone) -> DB object is null, this shouldn't happen.");


                        player.Stats.PB[style].Ticks =
                            player.Timer.Ticks; // Reload the run_time for the HUD and also assign for the DB query

#if DEBUG
                        _logger.LogDebug($@"CS2 Surf DEBUG >> OnTriggerStartTouch (Map end zone) -> 
                            ============== INSERT INTO `MapTimes` 
                            (`player_id`, `map_id`, `style`, `type`, `stage`, `run_time`, `start_vel_x`, `start_vel_y`, `start_vel_z`, `end_vel_x`, `end_vel_y`, `end_vel_z`, `run_date`) 
                            VALUES ({player.Profile.ID}, {_gameManager.CurrentMap.ID}, {style}, 0, 0, {player.Stats.ThisRun.Ticks}, 
                            {player.Stats.ThisRun.StartVelX}, {player.Stats.ThisRun.StartVelY}, {player.Stats.ThisRun.StartVelZ}, {velocity_x}, {velocity_y}, {velocity_z}, {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()})
                            ON DUPLICATE KEY UPDATE run_time=VALUES(run_time), start_vel_x=VALUES(start_vel_x), start_vel_y=VALUES(start_vel_y), 
                            start_vel_z=VALUES(start_vel_z), end_vel_x=VALUES(end_vel_x), end_vel_y=VALUES(end_vel_y), end_vel_z=VALUES(end_vel_z), run_date=VALUES(run_date);
                        ");
#endif

                        // Add entry in DB for the run
                        if (!player.Timer.IsPracticeMode)
                        {
                            _plugin.AddTimer(1.5f, () =>
                            {
                                player.Stats.ThisRun.SaveMapTime(player, _database); // Save the MapTime PB data
                                player.Stats.LoadMapTimesData(player,
                                    _database); // Load the MapTime PB data again (will refresh the MapTime ID for the Checkpoints query)
                                player.Stats.ThisRun
                                    .SaveCurrentRunCheckpoints(player, _database); // Save this run's checkpoints
                                player.Stats
                                    .LoadCheckpointsData(
                                        _database); // Reload checkpoints for the run - we should really have this in `SaveMapTime` as well but we don't re-load PB data inside there so we need to do it here
                                _gameManager.CurrentMap.GetMapRecordAndTotals(_database); // Reload the Map record and totals for the HUD
                            });

                            // This section checks if the PB is better than WR
                            if (player.Timer.Ticks < _gameManager.CurrentMap.WR[player.Timer.Style].Ticks ||
                                _gameManager.CurrentMap.WR[player.Timer.Style].ID == -1)
                            {
                                int WrIndex =
                                    _gameManager.CurrentMap.ReplayBots.Count -
                                    1; // As the ReplaysBot is set, WR Index will always be at the end of the List
                                _plugin.AddTimer(2f, () =>
                                {
                                    _gameManager.CurrentMap.ReplayBots[WrIndex].Stat_MapTimeID =
                                        _gameManager.CurrentMap.WR[player.Timer.Style].ID;
                                    _gameManager.CurrentMap.ReplayBots[WrIndex].LoadReplayData(_database);
                                    _gameManager.CurrentMap.ReplayBots[WrIndex].ResetReplay();
                                });
                            }
                        }
                    }

#if DEBUG
                    player.Controller.PrintToChat(
                        $"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Red}Map Stop Zone");
#endif
                }

                // Map start zones -- hook into map_start, (s)tage1_start
                else if (trigger.Entity.Name.Contains("map_start") ||
                         trigger.Entity.Name.Contains("s1_start") ||
                         trigger.Entity.Name.Contains("stage1_start"))
                {
                    player.ReplayRecorder.Start(); // Start replay recording

                    player.Timer.Reset();
                    player.Stats.ThisRun.Checkpoint
                        .Clear(); // I have the suspicion that the `Timer.Reset()` does not properly reset this object :thonk:
                    player.Controller.PrintToCenter($"Map Start ({trigger.Entity.Name})");

#if DEBUG
                    player.Controller.PrintToChat(
                        $"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Green}Map Start Zone");
                    // player.Controller.PrintToChat($"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc -> KeyValues: {trigger.Entity.KeyValues3}");
#endif
                }

                // Stage start zones -- hook into (s)tage#_start
                else if (Regex.Match(trigger.Entity.Name, "^s([1-9][0-9]?|tage[1-9][0-9]?)_start$").Success)
                {
                    int stage = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1;
                    player.Timer.Stage = stage;

#if DEBUG
                    Console.WriteLine(
                        $"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Timer.IsRunning: {player.Timer.IsRunning}");
                    Console.WriteLine(
                        $"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> !player.Timer.IsStageMode: {!player.Timer.IsStageMode}");
                    Console.WriteLine(
                        $"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Stats.ThisRun.Checkpoint.Count <= stage: {player.Stats.ThisRun.Checkpoint.Count <= stage}");
#endif

                    // This should patch up re-triggering *player.Stats.ThisRun.Checkpoint.Count < stage*
                    if (player.Timer.IsRunning && !player.Timer.IsStageMode &&
                        player.Stats.ThisRun.Checkpoint.Count < stage)
                    {
                        player.Timer.Checkpoint = stage; // Stage = Checkpoint when in a run on a Staged map

#if DEBUG
                        Console.WriteLine(
                            $"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `stage`: {Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1}");
                        Console.WriteLine(
                            $"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Stage start zones) -> player.Stats.PB[{style}].Checkpoint.Count = {player.Stats.PB[style].Checkpoint.Count}");
#endif

                        // Print checkpoint message
                        player.HUD.DisplayCheckpointMessages(_gameManager.PluginPrefix);

                        // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
                        Checkpoint cp2 = new Checkpoint(stage,
                            player.Timer.Ticks,
                            velocity_x,
                            velocity_y,
                            velocity_z,
                            -1.0f,
                            -1.0f,
                            -1.0f,
                            -1.0f,
                            0);
                        player.Stats.ThisRun.Checkpoint[stage] = cp2;
                    }

#if DEBUG
                    player.Controller.PrintToChat(
                        $"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.Yellow}Stage {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Start Zone");
#endif
                }

                // Map checkpoint zones -- hook into map_(c)heck(p)oint#
                else if (Regex.Match(trigger.Entity.Name, "^map_c(p[1-9][0-9]?|heckpoint[1-9][0-9]?)$").Success)
                {
                    int checkpoint = Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value);
                    player.Timer.Checkpoint = checkpoint;

                    // This should patch up re-triggering *player.Stats.ThisRun.Checkpoint.Count < checkpoint*
                    if (player.Timer.IsRunning && !player.Timer.IsStageMode &&
                        player.Stats.ThisRun.Checkpoint.Count < checkpoint)
                    {
#if DEBUG
                        Console.WriteLine(
                            $"============== Initial entity value: {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} | Assigned to `checkpoint`: {Int32.Parse(Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value) - 1}");
                        Console.WriteLine(
                            $"CS2 Surf DEBUG >> CBaseTrigger_StartTouchFunc (Checkpoint zones) -> player.Stats.PB[{style}].Checkpoint.Count = {player.Stats.PB[style].Checkpoint.Count}");
#endif

                        // Print checkpoint message
                        player.HUD.DisplayCheckpointMessages(_gameManager.PluginPrefix);

                        // store the checkpoint in the player's current run checkpoints used for Checkpoint functionality
                        Checkpoint cp2 = new Checkpoint(checkpoint,
                            player.Timer.Ticks,
                            velocity_x,
                            velocity_y,
                            velocity_z,
                            -1.0f,
                            -1.0f,
                            -1.0f,
                            -1.0f,
                            0);
                        player.Stats.ThisRun.Checkpoint[checkpoint] = cp2;
                    }

#if DEBUG
                    player.Controller.PrintToChat(
                        $"CS2 Surf DEBUG >> CBaseTrigger_{ChatColors.Lime}StartTouchFunc{ChatColors.Default} -> {ChatColors.LightBlue}Checkpoint {Regex.Match(trigger.Entity.Name, "[0-9][0-9]?").Value} Zone");
#endif
                }
            }

            return HookResult.Continue;
        }
    }
}