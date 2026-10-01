-- Initialization script for PostgreSQL database
-- Matches schema expected by Godot SessionManager.cs

CREATE TABLE IF NOT EXISTS game_sessions (
    id SERIAL PRIMARY KEY,
    character VARCHAR(100) NOT NULL,
    current_level INT NOT NULL DEFAULT 1,
    total_score INT NOT NULL DEFAULT 0,
    collectables INT NOT NULL DEFAULT 0,
    created_at VARCHAR(100) NOT NULL,
    last_played VARCHAR(100) NOT NULL
);
