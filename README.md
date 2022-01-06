# Mouse Click Simulator for TT Rewritten

This is a new implementation of the older **TT Mouse Click Simulator** that is intended to work with Toontown Rewritten. It is implemented in C# and runs on Windows on .NET Framework 4.6 or higher.

The TTR Mouse Click Simulator is able to automatically fish in specific locations like Punchling Place, TTC. To accomplish this, it scans the screen to detect fish bubbles, calculates how far the rod must be cast to catch the fish, and moves the Toon to the fisherman to sell the fish.

A recent addition for gardening is the the ability to plant flowers using 1 to 8 jellybean combinations, which is implemented using "Quick Actions". By clicking on a button with the flower's name, the Mouse Click Simulator will plant the flower by selecting the correct jellybean combination.

You can watch a video of the <a href="https://www.youtube.com/watch?v=uq7VaJkO6-k" target="_blank">Automatic Fishing Function for Tenor Terrace</a> and <a href="https://www.youtube.com/watch?v=dS-gBcvsjz4" target="_blank">Punchline Place</a>.

![](https://user-images.githubusercontent.com/13289184/148388183-a2010232-dec5-4d50-9893-0d9994b6ac17.png)

Note: This Simulator does not inject code into or otherwise manipulate the game. It only interacts with TTR by creating screenshots to analyze the window content 
(for the fishing action) and simulating mouse clicks/movements and pressing keys.

When enabling **Background Mode**, the simulator directly sends mouse and keyboard inputs to the Toontown window (instead of simulating gobal inputs),
so you can do other work while the simulator is running. The Toontown window can even be hidden behind other apps.

## WARNING
Use this program at your own risk!
Toontown Rewritten states in their Terms of Use that you should not use automation software, so you might risk a ban if you use this program.

## Running the Mouse Click Simulator

Please see the topic [Running the Simulator](https://github.com/kpreisser/MouseClickSimulator/wiki/Running-the-Simulator) for guidance how to download, build and run the Mouse Click Simulator on your computer.

## Development Status

Currently, the implementation contains actions for pressing keys, writing text, SpeedChat, Doodle Interaction Panel and the Automatic Fishing Function. Furthermore, an action for planting a flower is supported.

The GUI allows to load projects from an XML file. There are some predefined projects included in the **SampleProjects** folder but you can also create your own XML Simulator Project files. You can use the [**Sample Actions.xml**](https://github.com/kpreisser/MouseClickSimulator/blob/master/TTMouseclickSimulator/SampleProjects/Sample%20Actions.xml) as a template for creating your own project.

When opening a project, the GUI shows the actions which the project contains in a tree-like structure. When the Simulator is running, actions that are currently active are marked blue.

In addition to a "Main Action" which usually runs in a loop and can be started by clicking on the generic "Start" button (e.g. the fishing function), the Mouse Click Simulator supports "Quick Actions" 
that can be short and non-repeating, e.g. actions to plant specific flowers. For each Quick Action, a button is created which will start the corresponsing Quick Action when clicking on it.

### TODOs:
- Document how to use the Mouse Click Simulator.
- Document how to create own XML Simulator Project files.
