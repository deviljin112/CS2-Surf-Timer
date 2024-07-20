using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using SurfTimer.ST_DB;
using SurfTimer.ST_Game;
using SurfTimer.ST_Map;
using SurfTimer.ST_Utils;

namespace SurfTimer.ST_Events;

public class MapEvents
{
    private readonly ILogger<SurfTimer> _logger;
    private readonly SurfTimer _plugin;
    private readonly GameManager _gameManager;
    private readonly TimerDatabase _database;

    public MapEvents(ILogger<SurfTimer> logger, SurfTimer plugin, GameManager gameManager, TimerDatabase database)
    {
        _logger = logger;
        _plugin = plugin;
        _gameManager = gameManager;
        _database = database;
    }

    public void Init()
    {
        _plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        _plugin.RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }

    private void OnMapStart(string mapName)
    {
        if ((_gameManager.CurrentMap == null || _gameManager.CurrentMap.Name != mapName) && mapName.Contains("surf_"))
        {
            _plugin.AddTimer(1.0f, () => _gameManager.SetMap(new Map(mapName, _database)));
        }
    }

    private void OnMapEnd()
    {
        _gameManager.Reset();
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        ConVarHelper.RemoveCheatFlagFromConVar("bot_stop");
        ConVarHelper.RemoveCheatFlagFromConVar("bot_freeze");
        ConVarHelper.RemoveCheatFlagFromConVar("bot_zombie");

        Server.ExecuteCommand("execifexists SurfTimer/server_settings.cfg");
        _logger.LogInformation("[CS2 Surf] Executed configuration: server_settings.cfg");
        return HookResult.Continue;
    }
}