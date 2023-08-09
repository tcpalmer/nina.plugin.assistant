# Release Notes

## 3.3.0.0 - 2023-08-XX

### Exposure Ordering

TBD

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
