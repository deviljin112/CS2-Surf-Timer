using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using Microsoft.Extensions.Logging;
using SurfTimer.ST_DB;
using SurfTimer.ST_Game;

namespace SurfTimer.ST_Commands;

public class MapCommands
{
    private readonly ILogger<SurfTimer> _logger;
    private readonly SurfTimer _plugin;
    private readonly GameManager _gameManager;

    public MapCommands(ILogger<SurfTimer> logger, SurfTimer plugin, GameManager gameManager)
    {
        _logger = logger;
        _plugin = plugin;
        _gameManager = gameManager;
    }

    public void Init()
    {
        _plugin.AddCommand("css_tier", "Display the current map tier.", MapTier);
        _plugin.AddCommand("css_mapinfo", "Display the current map tier.", MapTier);
        _plugin.AddCommand("css_mi", "Display the current map tier.", MapTier);
        _plugin.AddCommand("css_triggers", "List all valid zone triggers in the map.", Triggers);
    }

    // All map-related commands here
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void MapTier(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        if (_gameManager.CurrentMap?.Stages > 1)
            player.PrintToChat(
                $"{_gameManager.PluginPrefix} {_gameManager.CurrentMap.Name} - {ChatColors.Green}Tier {_gameManager.CurrentMap.Tier}{ChatColors.Default} - Staged {ChatColors.Yellow}{_gameManager.CurrentMap.Stages} Stages{ChatColors.Default}");
        else
            player.PrintToChat(
                $"{_gameManager.PluginPrefix} {_gameManager.CurrentMap.Name} - {ChatColors.Green}Tier {_gameManager.CurrentMap.Tier}{ChatColors.Default} - Linear {ChatColors.Yellow}{_gameManager.CurrentMap.Checkpoints} Checkpoints{ChatColors.Default}");
    }

    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void Triggers(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        IEnumerable<CBaseTrigger> triggers = Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>("trigger_multiple");
        player.PrintToChat($"Count of triggers: {triggers.Count()}");
        foreach (CBaseTrigger trigger in triggers)
        {
            if (trigger.Entity!.Name != null)
            {
                player.PrintToChat(
                    $"Trigger -> Origin: {trigger.AbsOrigin}, Radius: {trigger.Collision.BoundingRadius}, Name: {trigger.Entity!.Name}");
            }
        }

        player.PrintToChat(
            $"Hooked Trigger -> Start -> {_gameManager.CurrentMap?.StartZone} -> Angles {_gameManager.CurrentMap.StartZoneAngles}");
        player.PrintToChat($"Hooked Trigger -> End -> {_gameManager.CurrentMap.EndZone}");
        int i = 1;
        foreach (Vector stage in _gameManager.CurrentMap.StageStartZone)
        {
            if (stage.X == 0 && stage.Y == 0 && stage.Z == 0)
                continue;
            player.PrintToChat(
                $"Hooked Trigger -> Stage {i} -> {stage} -> Angles {_gameManager.CurrentMap.StageStartZoneAngles[i]}");
            i++;
        }

        i = 1;
        foreach (Vector bonus in _gameManager.CurrentMap.BonusStartZone)
        {
            if (bonus.X == 0 && bonus.Y == 0 && bonus.Z == 0)
                continue;
            player.PrintToChat(
                $"Hooked Trigger -> Bonus {i} -> {bonus} -> Angles {_gameManager.CurrentMap.BonusStartZoneAngles[i]}");
            i++;
        }
    }
}