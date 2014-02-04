SysMana
=======

SysMana is a Windows system monitor designed to be easy to use and flexible, enabling users to construct a varied set of monitors that display data using images, animated gifs, graphs, etc.

![Screenshot: SysMana](http://i.imgur.com/CMaGFS7.gif)

Currently, the program can monitor the following resources:
* CPU usage
* RAM usage
* Hard drive space
* Recycle bin size / number of files
* Battery power (% remaining / time remaining)
* Download / upload speed
* Wireless signal strength
* System volume
* Audio peak level (left channel / right channel / master)
* Read data from a text file

To display the data SysMana has a variety of methods:
* Text output - the most basic of displays
* Spinner - the greater the data value the faster an image spins
* Progress bar - draws a foreground image stretching over a background image (in any direction, e.g. left to right, bottom to top, etc.)
* Image sequence - chooses a display image from a numbered sequence of pictures
* Graph - displays the data value's change over time (supports graph background & foreground textures)


Example monitor set
---------------------

The top image demonstrates the power, variability, and fun SysMana offers. The monitors used are, in order:
* CPU usage (Spinner)
* RAM available (Bottom to top progress bar)
* Available disk space (Bottom to top progress bar)
* System volume (Bottom to top progress bar)
* Battery % remaining (Bottom to top progress bar)
* Wireless signal strength (Image sequence)
* Download speed (Graph)
* Recycle Bin size (Bottom to top progress bar)
* Data file (Image sequence)

The last monitor reads the current weather status / moon phase from a file and displays the relevant image. The status is generated by another program I made: Wallcreeper.


Installation
-------------

1. (You need to have [.NET framework](http://www.microsoft.com/en-us/download/details.aspx?id=30653) installed on your computer)

2. Download either [the blank release](https://github.com/Winterstark/SysMana/releases)
 or [the above example release](https://github.com/Winterstark/SysMana/releases)

3. Extract

4. Run SysMana.exe


Usage
------

### Setup

To open the Setup window right-click on SysMana and select the Setup option. From there you can add, edit, or delete meters, as well as modify other options. See the next subsections for more details.

#### Blank Release

The blank release has no resource meters when it first runs, so you will see only a blank rectangle. Right-click it and select setup to begin adding meters to it. Just click Add and then set the new meter's data source and visualization.

#### Example Release

This release already has a bunch of resource meters set up; most of them don't need any further modification except for:
* The download speed graph (third from the bottom): you need to select the network adapter to monitor (if you're unsure try one and see if it registers any traffic). Also you need to specify your maximum download speed (in kb/s) under max. value.
* The weather status / text file (last one): if you don't use [Wallcreeper]() then this meter won't work at all; if you do, you need to specify the path to the "weather_status.txt" file in Wallcreeper's directory.

#### Adding meters

Meters are added in the SysMana Setup window by clicking the Add button. This will create a new line reading "CPU usage" (the default meter); moving the line up or down will change the order of the meters (left to right).

The right side of the window contains many options to customize the meter:
- Increasing left margin will distance the meter from the previous one. Negative values are also accepted, allowing two meters to overlap (or be positioned one above the other).
- Top margin is the distance from the top of the window to the meter.
- Data source is the resource the meter will be monitoring. Some data sources require the specification of a subsource, for example the Available disk space meter needs to know which disk to monitor.
- Min. and max. values define the interval that will contain the data values. These values are usually automatically set, except in a few cases, like download and upload speed (which you need to set manually).
- Visualization is the way SysMana will render the data. Each visualization presents its own set of options:
  - Text: prefix and postfix strings will be displayed with the data value.
  - Spinner: min. and max. spin speed are used to calculate how fast the image needs to spin based on the data value.
  - Progress bar: the foreground image will be drawn over the background image; their ratio is determined by the data value. The Progress vector defines the axis and direction the foreground will use to spread over the background.
  - Image sequence: images are added to the sequence with the Add images button. To remove or edit existing images click the Open images directory button and perform any modifications manually, then click the Reload button.
  - Graph: width is the total width of the graph; step width is the horizontal distance between two points on the graph; line width is the thickness of the graph line. Height is the total height of the graph. Step interval determines how often the graph will refresh. The texture is the image used to fill up either the top or bottom half of the graph.
- To resize the images in this meter use the Image zoom option.
- Click, Drag and drop, and Mouse wheel actions fire when the user interacts with the meter.

![Screenshot: Options window](http://i.imgur.com/dEzYx9S.png)

## Other options

To change SysMana's location just move it around the screen (it will remember the new position).

The right-click context menu has the option On Top: this makes SysMana on top of all other windows and is necessary to have if you want to position the meters over your taskbar. It temporarily turns off when the user is viewing a fullscreen application, or when the user clicks anywhere on the taskbar.

The General Options tab under SysMana Setup has access to other options, such as: background transparency, text font, vertical alignment, running SysMana when Windows boots, etc.

### Actions

Each resource meter can perform an action when you click on them:
* Open Task Manager, Control Panel, Recycle Bin, Power Options, etc.
* Run program/file
* Open webpage

A meter can also perform an action when you drag & drop a file onto it:
* Send the file to the Recycle Bin
* Copy or move the file to specified directory
* Run the file

You can enable volume control via the mouse wheel, which is useful when combined with a meter that displays the current volume.

Note that you can view the exact data value by moving the mouse cursor over a meter.


Credits
----------

SysMana Icon by [Raindropmemory](http://raindropmemory.deviantart.com/art/Legendora-Icon-Set-118999011).

Example monitor set images:
* Steampunk gear found [here](http://www.gjillianstone.com/the-yard-men.html)
* Diablo III orbs from [Diablo Gamepedia](http://diablo.gamepedia.com/Category:Diablo_III_User_Interface_Images)
* Vault Boy from [Fallout wikia](http://fallout.wikia.com/wiki/Category:Fallout_3_achievement_and_trophy_images)
* Skyrim icons by [SickAbomination](http://sickabomination.deviantart.com/art/Skyrim-Orb-270815282)
* Animated weather icons by [Rasheed Sobhee](http://www.behance.net/gallery/Weather-Animation-Icons-Free-Download/10740083)
* Moon phase icons by [VClouds](http://vclouds.deviantart.com/art/VClouds-Weather-2-179058977)