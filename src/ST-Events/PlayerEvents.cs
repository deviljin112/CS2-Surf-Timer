using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;
using SurfTimer.ST_DB;
using SurfTimer.ST_Game;
using SurfTimer.ST_Player;

namespace SurfTimer.ST_Events;

public class PlayerEvents
{
    private readonly ILogger<SurfTimer> _logger;
    private readonly SurfTimer _plugin;
    private readonly GameManager _gameManager;
    private readonly TimerDatabase _database;

    public PlayerEvents(ILogger<SurfTimer> logger, SurfTimer plugin, GameManager gameManager, TimerDatabase database)
    {
        _logger = logger;
        _plugin = plugin;
        _gameManager = gameManager;
        _database = database;
    }

    public void Init()
    {
        _plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        _plugin.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        _plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var controller = @event.Userid;
        if (!controller.IsValid || !controller.IsBot)
            return HookResult.Continue;

        for (int i = 0; i < _gameManager.CurrentMap?.ReplayBots.Count; i++)
        {
            if (_gameManager.CurrentMap.ReplayBots[i].IsPlayable)
                continue;

            int repeats = -1;
            if (_gameManager.CurrentMap.ReplayBots[i].Stat_Prefix == "PB")
                repeats = 3;

            _gameManager.CurrentMap.ReplayBots[i].SetController(controller, repeats);
            Server.PrintToChatAll($"{ChatColors.Lime} Loading replay data...");
            _plugin.AddTimer(2f, () =>
            {
                if (!_gameManager.CurrentMap.ReplayBots[i].IsPlayable)
                    return;

                _gameManager.CurrentMap.ReplayBots[i].Controller!.RemoveWeapons();

                _gameManager.CurrentMap.ReplayBots[i].LoadReplayData(_database);

                _gameManager.CurrentMap.ReplayBots[i].Start();
            });

            return HookResult.Continue;
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        _logger.LogDebug(
            $"CS2 Surf DEBUG >> OnPlayerConnect -> {player.PlayerName} / {player.UserId} / {player.SteamID}");
        _logger.LogDebug(
            $"CS2 Surf DEBUG >> OnPlayerConnect -> {player.PlayerName} / {player.UserId} / Bot Diff: {player.PawnBotDifficulty}");

        if (player.IsBot ||
            !player.IsValid) // IsBot might be broken so we can check for PawnBotDifficulty which is `-1` for real players
        {
            return HookResult.Continue;
        }
        else
        {
            int dbId, joinDate, lastSeen, connections;
            string name, country;

            // GeoIP
            DatabaseReader geoIpDb = new DatabaseReader(_gameManager.PluginPath + "data/GeoIP/GeoLite2-Country.mmdb");
            try
            {
                if (geoIpDb.Country(player.IpAddress!.Split(":")[0]).Country.IsoCode is not null)
                {
                    country = geoIpDb.Country(player.IpAddress!.Split(":")[0]).Country.IsoCode!;

                    _logger.LogDebug(
                        $"CS2 Surf DEBUG >> OnPlayerConnect -> GeoIP -> {player.PlayerName} -> {player.IpAddress!.Split(":")[0]} -> {country}");
                }
                else
                    country = "XX";
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);
                country = "XX";
            }

            geoIpDb.Dispose();

            if (_database == null)
                throw new Exception("CS2 Surf ERROR >> OnPlayerConnect -> DB object is null, this shouldnt happen.");

            // Load player profile data from database (or create an entry if first time connecting)
            Task<MySqlDataReader> dbTask =
                _database.Query($"SELECT * FROM `Player` WHERE `steam_id` = {player.SteamID} LIMIT 1;");
            MySqlDataReader playerData = dbTask.Result;
            if (playerData.HasRows && playerData.Read())
            {
                // Player exists in database
                dbId = playerData.GetInt32("id");
                name = playerData.GetString("name");
                if (country == "XX" && playerData.GetString("country") != "XX")
                    country = playerData.GetString("country");
                joinDate = playerData.GetInt32("join_date");
                lastSeen = playerData.GetInt32("last_seen");
                connections = playerData.GetInt32("connections");
                playerData.Close();

                _logger.LogDebug(
                    $"CS2 Surf DEBUG >> OnPlayerConnect -> Returning player {name} ({player.SteamID}) loaded from database with ID {dbId}");
            }
            else
            {
                playerData.Close();
                // Player does not exist in database
                name = player.PlayerName;
                joinDate = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                lastSeen = joinDate;
                connections = 1;

                // Write new player to database
                Task<int> newPlayerTask = _database.Write($@"
                    INSERT INTO `Player` (`name`, `steam_id`, `country`, `join_date`, `last_seen`, `connections`) 
                    VALUES ('{MySqlHelper.EscapeString(name)}', {player.SteamID}, '{country}', {joinDate}, {lastSeen}, {connections});
                ");
                int newPlayerTaskRows = newPlayerTask.Result;
                if (newPlayerTaskRows != 1)
                    throw new Exception(
                        $"CS2 Surf ERROR >> OnPlayerConnect -> Failed to write new player to database, this shouldnt happen. Player: {name} ({player.SteamID})");
                newPlayerTask.Dispose();

                // Get new player's database ID
                Task<MySqlDataReader> newPlayerDataTask =
                    _database.Query($"SELECT `id` FROM `Player` WHERE `steam_id` = {player.SteamID} LIMIT 1;");
                MySqlDataReader newPlayerData = newPlayerDataTask.Result;
                if (newPlayerData.HasRows && newPlayerData.Read())
                {
#if DEBUG
                    for (int i = 0; i < newPlayerData.FieldCount; i++)
                    {
                        _logger.LogDebug(
                            $"CS2 Surf DEBUG >> OnPlayerConnect -> newPlayerData[{i}] = {newPlayerData.GetValue(i)}");
                    }
#endif
                    dbId = newPlayerData.GetInt32("id");
                }
                else
                    throw new Exception(
                        $"CS2 Surf ERROR >> OnPlayerConnect -> Failed to get new player's database ID after writing, this shouldnt happen. Player: {name} ({player.SteamID})");

                newPlayerData.Close();

#if DEBUG
                Console.WriteLine(
                    $"CS2 Surf DEBUG >> OnPlayerConnect -> New player {name} ({player.SteamID}) added to database with ID {dbId}");
#endif
            }

            dbTask.Dispose();

            // Create Player object and add to playerList
            PlayerProfile profile =
                new PlayerProfile(dbId, name, player.SteamID, country, joinDate, lastSeen, connections);

            _gameManager.PlayerList[player.UserId ?? 0] = new Player(player,
                new CCSPlayer_MovementServices(player.PlayerPawn.Value!.MovementServices!.Handle),
                profile, _gameManager.CurrentMap);

            _logger.LogInformation(_gameManager.PlayerList.ToString());
#if DEBUG
            _logger.LogDebug(
                $"=================================== SELECT * FROM `MapTimes` WHERE `player_id` = {_gameManager.PlayerList[player.UserId ?? 0].Profile.ID} AND `map_id` = {_gameManager.CurrentMap?.ID};");
#endif

            // To-do: hardcoded Style value
            // Load MapTimes for the player's PB and their Checkpoints
            _gameManager.PlayerList[player.UserId ?? 0].Stats
                .LoadMapTimesData(_gameManager.PlayerList[player.UserId ?? 0],
                    _database); // Will reload PB and Checkpoints for the player for all styles
            _gameManager.PlayerList[player.UserId ?? 0].Stats
                .LoadCheckpointsData(
                    _database); // To-do: This really should go inside `LoadMapTimesData` imo cuz here we hardcoding load for Style 0

            // Print join messages
            Server.PrintToChatAll(
                $"{_gameManager.PluginPrefix} {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has connected from {ChatColors.Lime}{_gameManager.PlayerList[player.UserId ?? 0].Profile.Country}{ChatColors.Default}.");
            _logger.LogInformation(
                $"[CS2 Surf] {player.PlayerName} has connected from {_gameManager.PlayerList[player.UserId ?? 0].Profile.Country}.");
            return HookResult.Continue;
        }
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;

        for (int i = 0; i < _gameManager.CurrentMap!.ReplayBots.Count; i++)
            if (_gameManager.CurrentMap.ReplayBots[i].IsPlayable &&
                _gameManager.CurrentMap.ReplayBots[i].Controller!.Equals(player))
                _gameManager.CurrentMap.ReplayBots[i].Reset();

        if (player.IsBot || !player.IsValid)
        {
            return HookResult.Continue;
        }


        if (_database == null)
            throw new Exception("CS2 Surf ERROR >> OnPlayerDisconnect -> DB object is null, this shouldnt happen.");

        if (!_gameManager.PlayerList.ContainsKey(player.UserId ?? 0))
        {
            Console.WriteLine(
                $"CS2 Surf ERROR >> OnPlayerDisconnect -> Player playerList does NOT contain player.UserId, this shouldn't happen. Player: {player.PlayerName} ({player.UserId})");
        }
        else
        {
            // Update data in Player DB table
            Task<int> updatePlayerTask = _database.Write($@"
                    UPDATE `Player` SET country = '{_gameManager.PlayerList[player.UserId ?? 0].Profile.Country}', 
                    `last_seen` = {(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, `connections` = `connections` + 1 
                    WHERE `id` = {_gameManager.PlayerList[player.UserId ?? 0].Profile.ID} LIMIT 1;
                ");
            if (updatePlayerTask.Result != 1)
                throw new Exception(
                    $"CS2 Surf ERROR >> OnPlayerDisconnect -> Failed to update player data in database. Player: {player.PlayerName} ({player.SteamID})");
            // Player disconnection to-do
            updatePlayerTask.Dispose();

            // Remove player data from playerList
            _gameManager.PlayerList.Remove(player.UserId ?? 0);
        }

        return HookResult.Continue;
    }
}