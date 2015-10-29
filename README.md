# Mouse Click Simulator for TT Rewritten

This is a new implementation of the [**TT Mouse Click Simulator**](http://old.preisser-it.de/tt-mausklick/) that is intended to work with TT Rewritten. It is implemented in C# and runs on Windows on .Net 4.6 or higher.

Among automating tasks like saying SpeedChat phrases to train a Doodle, the TTR Mouse Click Simulator is able to automatically fish in specific locations like Punchling Place, TTC. To accomplish this, it scans the screen to detect fish bubbles, calculates how far the rod must be thrown to catch the fish, and moves the Toon to the fisherman to sell the fish.

![simulatorscreenshot](https://cloud.githubusercontent.com/assets/15179430/10716090/24ac7f16-7b2d-11e5-88cc-52511b380df2.png)

### WARNING
Use this program at your own risk!
Toontown Rewritten states in their Terms of Use that you should not use automation software, so you might risk a ban if you use this program.

## Running the Mouse Click Simulator

Please see the topic [Running the Simulator](https://github.com/TTExtensions/MouseClickSimulator/wiki/Running-the-Simulator) for guidance how to download, build and run the Mouse Click Simulator on your computer.

## Development Status

Currently, the implementation contains actions for pressing keys, writing text, SpeedChat, Doodle Interaction Panel and the Automatic Fishing Function.

The GUI allows to load projects from an XML file. There are some predefined projects included in the **SampleProjects** folder but you can also create your own XML Simulator Project files. You can use the [**Sample Actions.xml**](https://github.com/TTExtensions/MouseClickSimulator/blob/master/TTMouseclickSimulator/SampleProjects/Sample%20Actions.xml) as a template for creating your own project.

When opening a project, the GUI shows the actions which the project contains in a tree-like structure. When the Simulator is running, actions that are currently active are marked blue.

### TODOs:
- Document how to use the Mouse Click Simulator.
- Document how to create own XML Simulator Porject files.
