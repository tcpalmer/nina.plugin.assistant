﻿using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("B4541BA9-7B07-4D71-B8E1-6C73D4933EA0")]

[assembly: AssemblyTitle("Target Scheduler")]
[assembly: AssemblyDescription("An automated target scheduler for NINA *BETA RELEASE*")]
[assembly: AssemblyCompany("Tom Palmer @tcpalmer")]
[assembly: AssemblyProduct("Assistant.NINAPlugin")]
[assembly: AssemblyCopyright("Copyright © 2023")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("0.8.0.0")]
[assembly: AssemblyFileVersion("0.8.0.0")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "2.1.0.9001")]

[assembly: AssemblyMetadata("License", "MPL-2.0")]
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("Repository", "https://github.com/tcpalmer/nina.plugin.assistant/")]
[assembly: AssemblyMetadata("FeaturedImageURL", "https://raw.githubusercontent.com/tcpalmer/nina.plugin.assistant/main/NINA.Plugin.Assistant/assets/target-scheduler-logo.png?raw=true")]
[assembly: AssemblyMetadata("ScreenshotURL", "https://raw.githubusercontent.com/tcpalmer/nina.plugin.assistant/main/NINA.Plugin.Assistant/assets/screenshot-1.png?raw=true")]
[assembly: AssemblyMetadata("AltScreenshotURL", "https://raw.githubusercontent.com/tcpalmer/nina.plugin.assistant/main/NINA.Plugin.Assistant/assets/screenshot-2.png?raw=true")]

[assembly: AssemblyMetadata("LongDescription", @"The Target Scheduler Plugin is designed to provide a higher level of automation than is typically achievable with NINA. Specifically, it maintains a database of imaging projects describing DSO targets and associated exposure plans. Based on various criteria and preferences, it can decide at any given time what project/target should be actively imaging. A user will enter the desired projects, targets, and preferences into a UI exposed by the plugin. At runtime, a single new instruction for the NINA Advanced Sequencer will interact with the planning engine to determine the best target for imaging at each point throughout a night. The instruction will manage the slew/center to the target, switching filters, taking exposures, and dithering - all while transparently interacting with the surrounding NINA triggers and conditions.

If you like this plugin and it saves you time, please consider [Buying Me a Beer](https://www.buymeacoffee.com/tcpalmer).

## Documentation
The [plugin documentation](https://tcpalmer.github.io/nina-scheduler/) provides a detailed description of the plugin and how to use it.  See the [release notes](https://tcpalmer.github.io/nina-scheduler/release.html) for changes in recent versions.

## Acknowledgements ##
* Chris Woodhouse has been involved since the beginning and was instrumental in hashing out the initial design and requirements.
* Matt Nudi also provided design feedback and critical early beta testing.
* The concept for the plugin was originally inspired by the AIC video [Tim Hutchison: Automating a Backyard Observatory](https://www.youtube.com/watch?v=a4IkAUZkXH0).
* Finally, it's a tribute to the elegant design of NINA and the Advanced Sequencer that a plugin of this type is not only possible, but can also interact correctly with other elements in a sequence.

# Getting Help #
* Review the [plugin documentation](https://tcpalmer.github.io/nina-scheduler/)
* Ask for help (tag @tcpalmer) in the #plugin-discussions channel on the NINA project [Discord server](https://discord.com/invite/rWRbVbw).
* [Plugin source code](https://github.com/tcpalmer/nina.plugin.assistant)
* [Change log](https://github.com/tcpalmer/nina.plugin.assistant/blob/main/CHANGELOG.md)

The Sequence Scheduler plugin is provided 'as is' under the terms of the [Mozilla Public License 2.0](https://github.com/tcpalmer/nina.plugin.assistant/blob/main/LICENSE.txt)
")]

[assembly: ComVisible(false)]
