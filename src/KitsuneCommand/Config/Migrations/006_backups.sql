-- Backups table for tracking world save backup archives
CREATE TABLE IF NOT EXISTS backups (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    filename TEXT NOT NULL,
    world_name TEXT NOT NULL,
    size_bytes INTEGER NOT NULL,
    created_at TEXT NOT NULL,
    backup_type TEXT NOT NULL DEFAULT 'manual',
    notes TEXT
);
