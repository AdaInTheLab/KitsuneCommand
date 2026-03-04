-- Add interval_minutes for simple interval-based scheduling
ALTER TABLE task_schedules ADD COLUMN interval_minutes INTEGER NOT NULL DEFAULT 60;
