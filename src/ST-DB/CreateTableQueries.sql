-- noinspection SqlNoDataSourceInspectionForFile

-- noinspection SqlDialectInspectionForFile

CREATE TABLE IF NOT EXISTS Player (id INT AUTO_INCREMENT PRIMARY KEY , steam_id BIGINT NOT NULL UNIQUE COMMENT 'Unique SteamID64' , name VARCHAR(32) , country VARCHAR(2) COMMENT 'ISO 3166-1 alpha-2' , join_date INT COMMENT 'Unix timestamp' , last_seen INT COMMENT 'Unix timestamp' , connections SMALLINT );

CREATE TABLE IF NOT EXISTS PlayerSettings (player_id INT PRIMARY KEY , setting VARCHAR(32) NOT NULL , value VARCHAR(64) NOT NULL , created_date INT COMMENT 'Unix timestamp' , last_modified INT COMMENT 'Unix timestamp' , FOREIGN KEY (player_id) REFERENCES Player(id));

CREATE TABLE IF NOT EXISTS PlayerStats (player_id INT PRIMARY KEY , style TINYINT , points INT , playtime INT COMMENT 'Minutes played' , FOREIGN KEY (player_id) REFERENCES Player(id));

CREATE TABLE IF NOT EXISTS Maps (id INT AUTO_INCREMENT PRIMARY KEY , tier TINYINT DEFAULT 0 , author VARCHAR(64) , stages TINYINT COMMENT '0 = linear, 1+ = count of stages' , bonuses TINYINT COMMENT '0 = linear, 1+ = count of bonuses' , ranked BOOLEAN , date_added INT COMMENT 'Unix timestamp' , last_played INT COMMENT 'Unix timestamp' );

CREATE TABLE IF NOT EXISTS MapTimes (id INT AUTO_INCREMENT PRIMARY KEY , player_id INT , map_id INT , style TINYINT , type TINYINT COMMENT '0 = map time, 1+ = bonus no. THIS MUST BE 0 IF stages > 0, WE DO NOT HAVE STAGES IN BONUSES.' , stage TINYINT COMMENT 'if type = 0: 0 = not staged, 1+ = stage no. THIS MUST BE 0 IF type > 0, WE DO NOT HAVE STAGES IN BONUSES.' , run_time INT , start_vel_x DECIMAL(8, 3) , start_vel_y DECIMAL(8, 3) , start_vel_z DECIMAL(8, 3) , end_vel_x DECIMAL(8, 3) , end_vel_y DECIMAL(8, 3) , end_vel_z DECIMAL(8, 3) , run_date INT COMMENT 'Unix timestamp' , replay_frames longblob COMMENT 'Replay frame data' , FOREIGN KEY (player_id) REFERENCES Player(id), FOREIGN KEY (map_id) REFERENCES Maps(id));

CREATE TABLE IF NOT EXISTS MapTimeInsights (maptime_id INT PRIMARY KEY , end_vel_x DECIMAL(8, 3) , end_vel_y DECIMAL(8, 3) , end_vel_z DECIMAL(8, 3) , attempts INT , FOREIGN KEY (maptime_id) REFERENCES MapTimes(id));

CREATE TABLE IF NOT EXISTS MapZones (id INT AUTO_INCREMENT PRIMARY KEY , map_id INT , type TINYINT COMMENT '0: start, 1: cp, 2: end, 10: stop timer, 11: speed limit, etc' , bonux TINYINT COMMENT 'Index of bonus, this < 1 is treated as no-bonus' , stage TINYINT COMMENT 'Index of stage, this < 1 is treated as no-stage' , trigger_hook VARCHAR(64) COMMENT 'Trigger name to hook for this zone. Any position data for the zone is ignored if this is not blank.' , max_zone_speed SMALLINT COMMENT 'Max speed you can leave this zone with. (Only for type: 0)' , FOREIGN KEY (map_id) REFERENCES Maps(id));

CREATE TABLE IF NOT EXISTS Checkpoints (maptime_id INT PRIMARY KEY , cp TINYINT , start_vel_x DECIMAL(8, 3) , start_vel_y DECIMAL(8, 3) , start_vel_z DECIMAL(8, 3) , end_vel_x DECIMAL(8, 3) , end_vel_y DECIMAL(8, 3) , end_vel_z DECIMAL(8, 3) , end_touch DECIMAL(12, 6) , attempts INT , FOREIGN KEY (maptime_id) REFERENCES MapTimes(id));