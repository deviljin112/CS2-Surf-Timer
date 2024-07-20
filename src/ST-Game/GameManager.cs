using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using SurfTimer.ST_Interfaces;
using SurfTimer.ST_Map;
using SurfTimer.ST_Player;

namespace SurfTimer.ST_Game;

public class GameManager
{
    private readonly ILogger<GameManager> _logger;
    public PluginConfig Config { get; private set; }
    public Dictionary<int, Player> PlayerList { get; private set; }
    public Map? CurrentMap { get; private set; }
    public string PluginPrefix => $"[{ChatColors.DarkBlue}{Config.PluginPrefix}{ChatColors.Default}]";
    public readonly string PluginPath = Server.GameDirectory + "/csgo/addons/counterstrikesharp/plugins/SurfTimer/";
    
    public GameManager(ILogger<GameManager> logger)
    {
        _logger = logger;
        PlayerList = new Dictionary<int, Player>();

        PluginConfig config = JsonSerializer.Deserialize<PluginConfig>(File.ReadAllText(Server.GameDirectory + "/csgo/cfg/SurfTimer/configuration.json"))!;
        Config = config;
    }

    public void SetMap(Map newMap)
    {
        CurrentMap = newMap;
        _logger.LogDebug($"{PluginPrefix} Map set to: {newMap}");
    }

    public void Reset()
    {
        CurrentMap = null!;
        PlayerList.Clear();
        _logger.LogDebug($"{PluginPrefix} Game has been reset");
    }
}