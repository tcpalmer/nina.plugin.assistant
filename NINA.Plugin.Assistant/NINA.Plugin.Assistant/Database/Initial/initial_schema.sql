/* */
CREATE TABLE IF NOT EXISTS `projectpreference` (
	`id`			INTEGER NOT NULL,
    `preferences`	TEXT NOT NULL,
	PRIMARY KEY(`id`)
);

CREATE TABLE IF NOT EXISTS `filterpreference` (
	`id`			INTEGER NOT NULL,
    `profileId`		TEXT NOT NULL,
    `filtername`	TEXT NOT NULL,
    `preferences`	TEXT NOT NULL,
	PRIMARY KEY(`id`)
);

CREATE TABLE IF NOT EXISTS `project` (
	`id`			INTEGER NOT NULL,
	`profileId`		TEXT NOT NULL,
	`preferences_id` INTEGER NOT NULL,
	`name`			TEXT NOT NULL,
	`description`	TEXT,
	`state`			INTEGER,
	`priority`		INTEGER,
	`createdate`	INTEGER,
	`activedate`	INTEGER,
	`inactivedate`	INTEGER,
	`startdate`		INTEGER,
	`enddate`		INTEGER,
	PRIMARY KEY(`id`)
);

CREATE TABLE IF NOT EXISTS `target` (
	`id`			INTEGER NOT NULL,
	`name`			TEXT NOT NULL,
	`ra`			REAL,
	`dec`			REAL,
	`epochcode`		INTEGER NOT NULL,
	`rotation`		REAL,
	`roi`			REAL,
	`project_id`	INTEGER,
	PRIMARY KEY(`id`),
	FOREIGN KEY(`project_id`) REFERENCES `project`(`id`)
);

CREATE TABLE IF NOT EXISTS `filterplan` (
	`id`			INTEGER NOT NULL,
	`filtername`	TEXT NOT NULL,
	`profileId`		TEXT NOT NULL,
	`exposure`		REAL NOT NULL,
	`gain`			INTEGER,
	`offset`		INTEGER,
	`bin`			INTEGER,
	`readoutmode`	INTEGER,
	`desired`		INTEGER,
	`acquired`		INTEGER,
	`accepted`		INTEGER,
	`targetid`		INTEGER,
	PRIMARY KEY(`id`),
	FOREIGN KEY(`targetid`) REFERENCES `target`(`id`)
);
