#define DEBUG

using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using Microsoft.Extensions.Logging;
using SurfTimer.ST_Commands;
using SurfTimer.ST_DB;
using SurfTimer.ST_Events;
using SurfTimer.ST_Game;
using SurfTimer.ST_Interfaces;

namespace SurfTimer;

[MinimumApiVersion(120)]
public class SurfTimer : BasePlugin
{
    public override string ModuleName => "CS2 SurfTimer";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleDescription => "Fork of the Official Surf Timer by the CS2 Surf Initiative.";
    public override string ModuleAuthor => "Original: github.com/CS2Surf Fork: Deviljin112";

    private readonly ILogger<SurfTimer> _logger;
    private readonly GameManager _gameManager;
    private readonly TimerDatabase _database;
    private TickEvents _tickEvents;
    private MapEvents _mapEvents;
    private PlayerEvents _playerEvents;
    private TriggerTouchEvents _triggerTouchEvents;
    private MapCommands _mapCommands;
    private PlayerCommands _playerCommands;

    public SurfTimer(ILogger<SurfTimer> logger, GameManager gameManager, TimerDatabase database)
    {
        _logger = logger;
        _gameManager = gameManager;
        _database = database;
    }

    public override void Load(bool hotReload)
    {
        DatabaseConfig dbConfig =
            JsonSerializer.Deserialize<DatabaseConfig>(
                File.ReadAllText(Server.GameDirectory + "/csgo/cfg/SurfTimer/database.json"))!;

        _database.Configure(
            dbConfig.host,
            dbConfig.database,
            dbConfig.user,
            dbConfig.password,
            dbConfig.port,
            dbConfig.timeout
        );
        _logger.LogInformation("[CS2 Surf] Database connection established.");

        _database.InitDb();
        _logger.LogInformation("[CS2 Surf] Database initialized.");

        _mapEvents = new MapEvents(_logger, this, _gameManager, _database);
        _mapEvents.Init();
        _tickEvents = new TickEvents(_logger, this, _gameManager);
        _tickEvents.Init();
        _triggerTouchEvents = new TriggerTouchEvents(_logger, this, _gameManager, _database);
        _triggerTouchEvents.Init();
        _playerEvents = new PlayerEvents(_logger, this, _gameManager, _database);
        _playerEvents.Init();
        _mapCommands = new MapCommands(_logger, this, _gameManager);
        _mapCommands.Init();
        _playerCommands = new PlayerCommands(_logger, this, _gameManager);
        _playerCommands.Init();

        _logger.LogInformation(
            String.Format("\n  ____________    ____         ___\n"
                          + " / ___/ __/_  |  / __/_ ______/ _/\n"
                          + "/ /___\\ \\/ __/  _\\ \\/ // / __/ _/ \n"
                          + "\\___/___/____/ /___/\\_,_/_/ /_/\n"
                          + $"[CS2 Surf] SurfTimer plugin loaded. Version: {ModuleVersion}"
            ));
    }
}