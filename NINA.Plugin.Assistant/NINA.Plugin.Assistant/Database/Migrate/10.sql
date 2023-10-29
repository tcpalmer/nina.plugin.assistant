/*
*/

ALTER TABLE profilepreference ADD COLUMN enableSynchronization INTEGER DEFAULT 0;
ALTER TABLE profilepreference ADD COLUMN syncWaitTimeout INTEGER DEFAULT 300;
ALTER TABLE profilepreference ADD COLUMN syncExposureTimeout INTEGER DEFAULT 300;

ALTER TABLE acquiredimage ADD COLUMN profileId TEXT;

PRAGMA user_version = 10;
