# Target Scheduler

## 0.7.0.0 - 2023-XX-XX
* Added support for meridian window restriction
* Added airmass to acquired image data detail display
* Fixed problem with ROI exposure capture
* Fixed problem with including rejected exposure plans

## 0.6.0.0 - 2023-04-27
* Added validation to detect when Loop Conditions or Instructions are added to the TS container

## 0.5.0.0 - 2023-04-26
* Added support for importing mosaic panels from Framing Assistant

## 0.4.1.0 - 2023-04-25
* Added support for managing profile preferences
* Added image grader reject reason to acquired image data

## 0.4.0.0 - 2023-04-24
* First cut at image grader

## 0.3.0.0 - 2023-04-21
* Removed start and end date fields from projects
* Created a custom log for the plugin
* Added support for database migration scripts
* Fixed bug with plan end time

## 0.2.0.1 - 2023-04-20
* Increased the timeout in the image save watcher for DB updates
* Fixed problem saving a sequence as a sequence template

## 0.2.0.0 - 2023-04-02
* Major refactoring of the plugin sequence containers.
* Added Setting Soonest scoring rule.  Although the database schema hasn't changed, any projects created prior to this release will not be able to use this rule.

