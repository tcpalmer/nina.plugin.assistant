/*
*/

ALTER TABLE profilepreference ADD COLUMN enableSynchronization INTEGER DEFAULT 0;

PRAGMA user_version = 10;
