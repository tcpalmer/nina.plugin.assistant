/* */
CREATE TABLE IF NOT EXISTS `preference` (
    `type`			INTEGER NOT NULL,
    `profileId`		INTEGER NOT NULL,
    `filterName`	TEXT NOT NULL,
    `preferences`	TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS `project` (
	`id`			INTEGER NOT NULL,
	`profileId`		INTEGER,
	`name`			TEXT NOT NULL,
	`description`	TEXT,
	`state`			INTEGER,
	`priority`		INTEGER,
	`createdate`	INTEGER,
	`activedate`	INTEGER,
	`inactivedate`	INTEGER,
	PRIMARY KEY(`id`)
);
CREATE TABLE IF NOT EXISTS `target` (
	`id`			INTEGER NOT NULL,
	`name`			TEXT NOT NULL,
	`ra`			REAL,
	`dec`			REAL,
	`rotation`		REAL,
	`roi`			REAL,
	`projectid`		INTEGER,
	PRIMARY KEY(`id`),
	FOREIGN KEY(`projectid`) REFERENCES `project`(`id`)
);
CREATE TABLE IF NOT EXISTS `exposureplan` (
	`id`			INTEGER NOT NULL,
	`filtername`	TEXT NOT NULL,
	`filterpos`		INTEGER NOT NULL,
	`exposure`		REAL NOT NULL,
	`gain`			INTEGER,
	`offset`		INTEGER,
	`bin`			INTEGER,
	`desired`		INTEGER,
	`acquired`		INTEGER,
	`accepted`		INTEGER,
	`targetid`		INTEGER,
	PRIMARY KEY(`id`),
	FOREIGN KEY(`targetid`) REFERENCES `target`(`id`)
);
