using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;
using SurfTimer.ST_Game;

namespace SurfTimer.ST_Events;

public class TickEvents
{
    private readonly ILogger<SurfTimer> _logger;
    private readonly SurfTimer _plugin;
    private readonly GameManager _gameManager;
    
    public TickEvents(ILogger<SurfTimer> logger, SurfTimer plugin, GameManager gameManager)
    {
        _logger = logger;
        _plugin = plugin;
        _gameManager = gameManager;
    }

    public void Init()
    {
        _plugin.RegisterListener<Listeners.OnTick>(OnTick);
    }
    
    public void OnTick()
    {
        foreach (var player in _gameManager.PlayerList.Values)
        {
            player.Timer.Tick();
            player.ReplayRecorder.Tick(player);
            player.HUD.Display();
        }

        if (_gameManager.CurrentMap == null)
            return;

        // Need to disable maps from executing their cfgs. Currently idk how (But seriously it's a security issue)
        ConVar? bot_quota = ConVar.Find("bot_quota");
        if (bot_quota != null)
        {
            int cbq = bot_quota.GetPrimitiveValue<int>();
            if(cbq != _gameManager.CurrentMap.ReplayBots.Count)
            {
                bot_quota.SetValue(_gameManager.CurrentMap.ReplayBots.Count);
            }
        }

        for(int i = 0; i < _gameManager.CurrentMap!.ReplayBots.Count; i++)
        {
            _gameManager.CurrentMap.ReplayBots[i].Tick();
            if (_gameManager.CurrentMap.ReplayBots[i].RepeatCount == 0)
                _gameManager.CurrentMap.KickReplayBot(i);
        }
    }
}