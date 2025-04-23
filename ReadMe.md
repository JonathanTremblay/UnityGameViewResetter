# Unity Game View Resetter

This Editor script resets the Game View (Aspect ratio 16:9, Scale 1, Low Resolution Aspect Ratios off).

## Table of Contents

1. [Getting Started](#getting-started)
2. [Technical Details](#technical-details)
3. [Compatibility](#compatibility)
4. [Known Issues](#known-issues)
5. [About the Project](#about-the-project)
6. [Contact](#contact)
7. [Version History](#version-history)
8. [License](#license)

## Getting Started

* Import this lightweight package to your project (or manually add the scripts to an Editor folder in the Assets folder).
* To use it, simply open your project.
* It executes the verification only once, when the Unity editor is loaded or when the script is added for the first time.
* It can also be executed manually from the Help menu.
* That's it!

## Technical Details

* This script is compatible with Windows and MacOS.

## Compatibility

* Tested on Windows and MacOS with Unity version 2022.3.17 and 6000.0.32.

## Known Issues

* Aspect Ratio is hard-coded to 16:9. In a future version, it should be a project setting.
* On MacOS, messages are always displayed in English. (On Windows, if the language settings are in French, messages are displayed in French.)
* (Issues can be reported on GitHub: https://github.com/JonathanTremblay/UnityGameViewResetter/issues)

## About the Project

* This tool is useful when you want to prevent team members from having different aspect ratios and zoom settings.
* It's also useful to get rid of the Low Resolution Aspect ratios when working with retina displays or Windows zoom settings.

## Contact

**Jonathan Tremblay**  
Teacher, Cegep de Saint-Jerome  
jtrembla@cstj.qc.ca

Project Repository: https://github.com/JonathanTremblay/UnityGameViewResetter

## Version History

* 0.9.4
    * Fixed a bug when there is no main camera.
* 0.9.3
    * Renamed asmdef file to match namespace. Limited reset to once per session.
* 0.9.2
    * Improved feedback and detection.
* 0.9.1
    * Fixed an issue to reset game view after project startup.
* 0.9.0
    * First public version.

## License

This tool is available for distribution and modification under the CC0 License, which allows for free use and modification.  
https://creativecommons.org/share-your-work/public-domain/cc0/