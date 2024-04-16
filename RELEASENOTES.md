# Release Notes

## 4.3.6.0 - 2024-04-17
* Added Target Scheduler Background Condition
* TS Container UI reworked to be more like a standard container and with better scrolling behavior (thanks Stefan)
* Fixed problem with override exposure order not being copied on paste operations and bulk import
* Fixed bug where internal filter name is unknown for OSC users
* Fixed bug (hopefully) where sync client was failing to process images and update the database

## 4.3.5.0 - 2024-03-08
* Fixed problem with CSV import due to NINA package updates

## 4.3.4.0 - 2024-02-23
* Added toggle in Projects navigation to color projects/targets by whether they are active or not
* Added toggle in Projects navigation to show/hide projects/targets by whether they are active or not
* Added copy/paste/reset for Project Scoring Rule Weights

## 4.3.3.0 - 2024-02-15
* Refactored target and exposure planning percent complete handling

## 4.3.2.1 - 2024-02-12
* Fixed exposure completion reversion caused by previous percent complete rule fix

## 4.3.2.0 - 2024-02-06
* Fixed bug in percent complete scoring rule for completed exposure plans

## 4.3.1.0 - 2024-02-02
* Another tweak to TS Condition to ensure loop remains completed
* Fixed bug where target from Framing Wizard would appear to replace target in TS target management panel
* Code clean up

## 4.3.0.0 - 2024-01-26
* Fixed issue where TS Condition wasn't working when called in outer containers
* Increased timeout for sync client registration
* Added validation of TS Container triggers and custom event containers
* Stopped cloning of TS Container triggers into plan sub-container (now run normally)
* Added additional logging of sequence item lifecycle events

## 4.2.0.0 - 2023-12-28
* Added ability to bulk load targets from CSV files

## 4.1.2.2 - 2023-12-21
* Fixed bug in readout mode handling
* Fixed bug with Percent Complete and Mosaic Complete scoring rules if image grading is off

## 4.1.2.0 - 2023-12-18
* Fixed bug in smart plan window - was skipping projects incorrectly
* Fixed another bug with determining target completed
* You can now choose to delete acquired image records when deleting the associated target
* If running as a sync client, TS Condition will now use the server's data for the targets remain or projects remain checks

## 4.1.1.3 - 2023-12-15
* Fixed bug in TS Flats with project flats cadence > 1
* Fixed bug with determining target completeness with exposure throttling
* Fixed missing TS version in TS log

## 4.1.1.1 - 2023-12-14
* Fixed bug in TS Condition - check wasn't running the first time through
* Immediate flats wasn't handling Repeat Flat Set off correctly
* Immediate flats instruction will now open a flip-flat cover when done
* Updated for latest NINA 3 beta libraries

## 4.1.0.8 - 2023-12-12
* Added support for taking automated flats
* Optimized the condition check in Target Scheduler condition
* Target Scheduler Container instruction has a new custom event container: _After Each Target_
* Added a 'need flats' check to Target Scheduler condition

## 4.0.5.1 - 2023-11-26
* Improved handling when TS is canceled/interrupted which means it behaves better in safety scenarios and with Powerups safety controls.

## 4.0.5.0 - 2023-11-17
* Added image grading on FWHM and Eccentricity (requires Hocus Focus plugin)
* Added option to move rejected images to a 'rejected' directory
* Added ability to purge acquired image records by date or date/target
* Added CSV output for acquired image records
* Added better support for the Center After Drift trigger (see release notes)
* Added smarter determination of plan stop times
* Added ability to delete all target exposure plans
* The rule weight list is now sorted when displayed
* Added target rotation and ROI to the set of data saved for acquired images.  A future release will use these values when selecting 'like' images for grading.
* Fixed issue where target rotation wasn't being sent to Framing Wizard
* Added experimental support for synchronization across multiple instances of NINA.  See the page on Synchronization in the documentation.
* All sequencer instructions moved to new category "Target Scheduler"

## 3.3.3.1 - 2023-10-11
* Fixed bug with exposure planner.

## 3.3.3.0 - 2023-09-19
* Fixed edge case bug with custom horizons.

## 3.3.2.0 - 2023-09-07
* Fixed problem with override exposure ordering. Unfortunately, any existing override order had to be cleared (automatically) for this fix.  You'll have to manually redo any that you had already created.

## 3.3.1.0 - 2023-08-22

### Exposure Ordering Override

You can now override the default exposure ordering (which is based on the Filter Switch Frequency and Dither settings on the project) and specify a manual override ordering, including dithers.

### Mosaic Completion Scoring Rule

Added a new rule to score mosaic projects based on completion ratio, intended to support balancing of exposures across panels.

This is supported by a new flag on Projects to indicate if they are for mosaics or not.  This defaults to false and can be manually changed to true.  It will also be automatically set to true if you import mosaic panels from the Framing Assistant.

### Other
* Fixed bug with rotation not being set when importing from a saved Sequence Target.
* Fixed bug related to non-existent custom horizon

## 3.2.1.0 - 2023-08-09
* Fixed bug preventing target ROI from being applied properly.

## 3.2.0.0 - 2023-08-07

### Minimum Altitude
Changed the behavior of project minimum altitude: now can be used with or without a custom horizon.  If used with, then the horizon at each azimuth is the greater of (custom horizon + horizon offset) or project minimum altitude.

### Copy/Paste Exposure Plans
You can now copy the exposure plans from one target and paste to another - even a target associated with a different profile.

### Acquired Images
* Added fixed date range options.
* Added ability to select images by filter used.
* Images in the table will now show 'not graded' as the Reject Reason if grading was disabled when the image completed.
* Improved search and display performance.

### Other
* Fixed issue with scheduler preview: wasn't picking up dynamic changes to target database.
* Added 5/10/20 minute options to project minimum time.
* Will automatically unpark the scope if parked before a target slew.
* Fixed the annoying bug related to editing Exposure Templates on Target Exposure Plans.
* Now skips useless Target Scheduler Condition checks.

## 3.1.2.0 - 2023-07-20

### Handling of Before/After Target

The execution of the Before/After Target containers was changed to mean run only for new or changed targets.

### Scheduler Preview Details

Scheduler Preview now provides a 'View Details' button to display details about the planning and decision-making process.

## 3.1.0.0 - 2023-07-13

### Custom Event Instructions

You can now drop arbitrary instructions into four separate containers that will be executed at specific times in the scheduler lifecycle:
- Before each Wait
- After each Wait
- Before each Target
- After each Target

For example, you could park your mount and/or close a flip-flat before a wait and then reverse after.

As part of this update, the default Conditions and Instructions drop areas in the Target Scheduler Container instruction were removed.  They weren't used and were just confusing.

### Display of Running Instructions

The display of running instructions in the Target Scheduler Container instruction has been greatly improved.

## 3.0.0.0 - 2023-07-XX
Ported to NINA 3.

### Target Rotation Angles
NINA 3 changed the meaning of target rotation to use the more standard 'counter clockwise notation'.  If you have non-zero rotation values for a target, they will be automatically converted.

## 0.8.0.0 - 2023-06-12

### Revised Dithering Approach
Previously, the 'Dither After Every' setting in Projects was the number of exposures before dithering would be triggered - regardless of filter.  This can lead to under-dithering in situations where the planner returns exposures for fewer filters than expected (e.g. due to exposure plan completion or moon avoidance).

Now, the setting means to 'dither after N instances of each filter'.  For example, if dither = 1 and the planner generates LRGBLRGBLRGBLLL, then dithers would be added to execute LRGBdLRGBdLRGBdLdLdL.  Previously, you might use dither = 4 in this situation but then once RGB is done, you'd be under-dithering the L exposures.

### Miscellaneous
* Fixed problem with missing parent for internal container
* Fixed problem where target rotation = 0 was ignored.  Now, if you're slewing and centering it will do a slew and center with rotation even if zero (assuming you have a rotator connected).


## 0.7.1.0 - 2023-05-25

### Meridian Window Support
This release adds support for restricting target imaging to a timespan around the target's meridian crossing in order to minimize airmass and light pollution impacts.

A new rule for the Scoring Engine lets you set the priority of targets using meridian windows so they can be prioritized if desired.

### Default Exposure Times

You can now add a default exposure time to your Exposure Templates.  This duration will be used unless overridden in Exposure Plans that use the template.

If you have existing Exposure Plans and want to use this feature:
* Add the desired default to your Exposure Templates.
* In your Exposure Plans, simply clear the existing exposure value - it should change to '(Template)' to indicate usage of the default.

### Scheduler Loop Condition

A new loop condition is provided to support outer sequence containers designed for safety concerns and/or multi-night operation.  The condition has two options:
* While Targets Remain Tonight: continue as long as the Planning Engine indicates that additional targets are available tonight (either now or by waiting).  This is the default.
* While Active Projects Remain: continue as long as any active Projects remain.

'While Active Projects Remain' should **ONLY** be used in an outer loop designed for multi-night operation with appropriate instructions to skip to the next dusk.  Since you may have active targets that can't be imaged for months, if you used this without skipping to the next day it would call the planner endlessly until the sequence was stopped manually.

### New Profile Preferences

* Option to park the mount when the planner is waiting for the next target.
* Option to throttle exposure counts when not using image grading.
* Option to accept all improvements in star count and/or HFR during image grading.


### Miscellaneous

* Added airmass to acquired image data detail display.
* Fixed problem with ROI exposure capture.
* Fixed problem with including rejected exposure plans.
* Fixed bug causing crashes during plan previews.
