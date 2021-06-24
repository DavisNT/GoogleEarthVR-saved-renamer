Saved places renamer for Google Earth VR
===============
[![Build status](https://ci.appveyor.com/api/projects/status/ppcrgpf3v57r9dms?svg=true)](https://ci.appveyor.com/project/DavisNT/googleearthvr-saved-renamer)  
A quick, dirty and portable tool for renaming saved places in Google Earth VR.

Version 1.1.2

Copyright (c) 2020-2021 Davis Mosenkovs

## Introduction

Google Earth VR allows to save places for later viewing. However it doesn't 
allow to specify names (titles) of the saved locations.

This tool allows to change title and subtitle of the saved locations by 
changing metadata in the saved files (located in `Google Earth VR` subfolder 
inside Pictures folder).

## Usage

The tool can be downloaded (after reading and accepting `LICENSE`) from 
this GitHub repository (e.g. [Releases](https://github.com/DavisNT/GoogleEarthVR-saved-renamer/releases) 
section or built from the source by using Visual Studio 2019).

The tool has the following command-line usages:
* `GEVR-SR file.jpg` - display current title, subtitle and Google Maps link to viewpoint location.
* `GEVR-SR file.jpg title` - set title without changing subtitle.
* `GEVR-SR file.jpg title subtitle` - set title and subtitle.
* `GEVR-SR /MsgBox file.jpg` - display current title and subtitle in a GUI (MessageBox) window. Console windows sometimes have issues displaying Unicode characters, the `/MsgBox` parameter works around this by displaying a GUI window.

## Notices

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

## More information

More information regarding Google Earth VR saved location file format can be found in: https://github.com/kbogert/kmz2gevr
