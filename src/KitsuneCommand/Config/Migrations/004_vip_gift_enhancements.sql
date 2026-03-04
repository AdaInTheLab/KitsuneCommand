-- Add claim_period for repeatable gifts (null=one-time, 'daily', 'weekly', 'monthly')
ALTER TABLE vip_gifts ADD COLUMN claim_period TEXT;

-- Add player_name for display in admin UI
ALTER TABLE vip_gifts ADD COLUMN player_name TEXT;
