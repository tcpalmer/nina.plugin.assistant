/* */

CREATE TABLE IF NOT EXISTS `project` (
	`Id`			INTEGER NOT NULL,
	`profileId`		TEXT NOT NULL,
	`name`			TEXT NOT NULL,
	`description`	TEXT,
	`state`			INTEGER,
	`priority`		INTEGER,
	`createdate`	INTEGER,
	`activedate`	INTEGER,
	`inactivedate`	INTEGER,
	`startdate`		INTEGER,
	`enddate`		INTEGER,
	`minimumtime`	INTEGER,
	`minimumaltitude`	REAL,
	`usecustomhorizon`	INTEGER,
	`horizonoffset`	REAL,
	`filterswitchfrequency`	INTEGER,
	`ditherevery`	INTEGER,
	`enablegrader`	INTEGER,
	PRIMARY KEY(`id`)
);

CREATE TABLE IF NOT EXISTS `target` (
	`Id`			INTEGER NOT NULL,
	`name`			TEXT NOT NULL,
	`ra`			REAL,
	`dec`			REAL,
	`epochcode`		INTEGER NOT NULL,
	`rotation`		REAL,
	`roi`			REAL,
	`projectid`		INTEGER,
	FOREIGN KEY(`projectId`) REFERENCES `project`(`Id`),
	PRIMARY KEY(`id`)
);

CREATE TABLE IF NOT EXISTS `filterplan` (
	`Id`			INTEGER NOT NULL,
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
	FOREIGN KEY(`targetId`) REFERENCES `target`(`Id`),
	PRIMARY KEY(`Id`)
);

CREATE TABLE IF NOT EXISTS `filterpreference` (
	`Id`			INTEGER NOT NULL,
    `profileId`		TEXT NOT NULL,
    `filtername`	TEXT NOT NULL,
	`twilightlevel` INTEGER,
	`moonavoidanceenabled`	INTEGER,
	`moonavoidanceseparation`	REAL,
	`moonavoidancewidth`	INTEGER,
	`maximumhumidity`	REAL,
	PRIMARY KEY(`Id`)
);

CREATE TABLE IF NOT EXISTS `ruleweight` (
	`Id`			INTEGER NOT NULL,
	`name`			TEXT NOT NULL,
    `weight`		REAL NOT NULL,
	`projectid`		INTEGER,
	FOREIGN KEY(`projectId`) REFERENCES `project`(`Id`)
	PRIMARY KEY(`Id`)
);

CREATE TABLE IF NOT EXISTS `acquiredimage` (
	`Id`			INTEGER NOT NULL,
	`targetId`		INTEGER NOT NULL,
	`acquireddate`	INTEGER,
	`filtername`	TEXT NOT NULL,
    `metadata`		TEXT NOT NULL,
	PRIMARY KEY(`Id`)
);
