SysMana
=======

SysMana is a Windows system monitor designed to be easy to use and flexible, enabling users to construct a varied set of monitors that display data using images, animated gifs, graphs, etc.

![Screenshot: SysMana](http://i.imgur.com/MsqNdvT.gif)

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
* Data from text file
* System time

To display the data SysMana has a variety of methods:
* Text output - the most basic of displays
* Spinner - the greater the data value the faster an image spins
* Progress bar - draws a foreground image stretching over a background image (in any direction, e.g. left to right, bottom to top, etc.)
* Image sequence - chooses a display image from a numbered sequence of pictures
* Graph - displays the data value's change over time
* Dota clock - only used to visualize system time; inspired by Dota 2

Keep in mind that SysMana might slow down your computer a little. After you set it up check Task Manager to see if it's using up too much CPU - you can reduce that number by increasing the Refresh interval in Options, and by eliminating some meters. Future versions of SysMana will probably be more optimized.


Example monitor set
---------------------

The resource monitors used in the top image are, in order:
1. RAM available (Bottom to top progress bar)
2. Available disk space (Bottom to top progress bar)
3. Data file (Bottom to top progress bar) - user's "stamina"
4. Battery % remaining (Bottom to top progress bar)
5. Download speed (Graph)
6. System volume (Radial progress bar)
7. Recycle Bin size (Bottom to top progress bar)
8. Data file (Image sequence) - current weather
9. System time (Dota clock)

Monitor #3 is connected to a program I made to remind me to take regular breaks from the computer ([Take5](https://github.com/Winterstark/Wallcreeper)). SysMana represents the time until the next break as an orb with slowly depleting stamina.

Monitor #8 reads the current weather status / moon phase from a file and displays the relevant image. The status is generated by my wallpaper manager: [Wallcreeper](https://github.com/Winterstark/Wallcreeper).


Installation
-------------

1. (You need to have [.NET framework](http://www.microsoft.com/en-us/download/details.aspx?id=30653) installed on your computer)
2. Download either [the blank release](https://github.com/Winterstark/SysMana/releases/tag/v1.0)
 or [the above example release](https://github.com/Winterstark/SysMana/releases/tag/v1.0-example)
3. Extract
4. Run SysMana.exe


Usage
------

### Setup

To open the Setup window right-click on SysMana and select the Setup option. From there you can add, edit, or delete meters, as well as modify other options. See the next sections for more details.

#### Blank Release

The blank release has no resource meters when it first runs, so you will see only a blank rectangle. Right-click it and select setup to begin adding meters to it. Just click Add and then set the new meter's data source and visualization.

#### Example Release

This release already has a bunch of resource meters set up; most of them don't need any further modification except for:
* Text file, third from the top (current "stamina" / time until next break): if you don't use [Take5](https://github.com/Winterstark/Take5) then this meter won't work at all; if you do, you need to specify the path to the "break_countdown.txt" file in Take5's directory.
* The download speed graph: you need to select the network adapter to monitor (if you're unsure try one and see if it registers any traffic). Also you need to specify your maximum download speed (in kb/s) under max. value.
* Text file, second from the bottom (weather status): if you don't use [Wallcreeper](https://github.com/Winterstark/Wallcreeper) then this meter won't work at all; if you do, you need to specify the path to the "weather_status.txt" file in Wallcreeper's directory.

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
  - Dota-style clock
- To resize the images in this meter use the Image zoom option.
- Click, Drag and drop, and Mouse wheel actions fire when the user interacts with the meter.

![Screenshot: Options window](http://i.imgur.com/dEzYx9S.png)

#### Dota-style clock

Dota-style clock is a data source/visualization of the current time inspired by Dota 2. Besides displaying the time it also shows an orb with two halves (representing day and night) that slowly rotates as the day goes by. The sunset and sunrise are calculated based on your latitude, longitude, and time zone (which you can set manually or search by location).

The two orb halves also change their relative size based on the actual day/night ratio, which slowly changes with the seasons. Hovering the mouse cursor over the clock will display the orb in its entirety, which allows you to see the current day/night ratio.

![Screenshot GIF: hovering the mouse over the clock](http://i.imgur.com/rC3FOcj.gif)

Four times a year (the equinoxes and solstices) the day/night orb is drawn using special textures. The clock also supports playing a sound when it is sunrise or sunset (by default the sounds are Dota 2 assets). All of the textures and sounds can be changed by replacing the files in the "imgs\dota_clock" folder.

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
