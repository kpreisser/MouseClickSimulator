# Mouse Click Simulator for Toontown Rewritten

This is a new implementation of the older **TT Mouse Click Simulator** that is intended to work with Toontown Rewritten. It is implemented
in C# (.NET 6) and runs on Windows.

The TTR Mouse Click Simulator is able to automatically fish in specific locations like Punchling Place, TTC. To accomplish this, it scans the
screen to detect fish bubbles, calculates how far the rod must be cast to catch the fish, and moves the Toon to the fisherman to sell the fish.

A recent addition for gardening is the the ability to plant flowers using 1 to 8 jellybean combinations, which is implemented using "Quick Actions".
By clicking on a button with the flower's name, the Mouse Click Simulator will plant the flower by selecting the correct jellybean combination.

You can watch a video of the <a href="https://www.youtube.com/watch?v=uq7VaJkO6-k" target="_blank">Automatic Fishing Function for Tenor Terrace</a>
and <a href="https://www.youtube.com/watch?v=dS-gBcvsjz4" target="_blank">Punchline Place</a>.

![](https://user-images.githubusercontent.com/13289184/148388183-a2010232-dec5-4d50-9893-0d9994b6ac17.png)

Note: This Simulator does not inject code into or otherwise manipulate the game. It only interacts with TTR by taking screenshots to analyze the window content
(for the fishing action) and simulating mouse clicks/movements and pressing keys.

When enabling **Background Mode**, the simulator directly sends mouse and keyboard inputs to the Toontown window (instead of simulating gobal inputs),
so you can do other work while the simulator is running.

## WARNING
Use this program at your own risk!
Toontown Rewritten states in their Terms of Use that you should not use automation software, so you might risk a ban if you use this program.

## Running the Mouse Click Simulator

Please see the topic [Running the Simulator](https://github.com/kpreisser/MouseClickSimulator/wiki/Running-the-Simulator) for guidance how to download,
build and run the Mouse Click Simulator on your computer.

## Release Notes
**2022-01-09** (Commit [8334b77](https://github.com/kpreisser/MouseClickSimulator/commit/bf78e3910d95b561cdbb0c34764dcb24dc648657))

- Updated to **.NET 6**.
  Due to this change, the prerequisites for building the simulator have slightly changed. Please see the topic 
  [Running the Simulator](https://github.com/kpreisser/MouseClickSimulator/wiki/Running-the-Simulator) for the new instructions.
- Added **Background Mode** which directly sends mouse and keyboard inputs to the Toontown window (instead of simulating gobal inputs).
  This allows you to do other work while the simulator is running.
- Added support for the 64-bit version of TT Rewritten ([#37](https://github.com/kpreisser/MouseClickSimulator/issues/37)).
- Added a new project for keeping the toon awake.
- Improved key press durations in the fishing projects, and added new projects for use during the winter theme.
- Fixed a bug that prevented the window location from being detected correctly when using multiple monitors with different DPI settings.

**2018-08-12** (Commit [67c4a87](https://github.com/kpreisser/MouseClickSimulator/commit/67c4a87c1db7fd906f3dfc88aa6cc26c51dc6d4f))

- The simulator now detects when multiple TT Rewritten windows are open, and allows to select the one that should be used
  ([#27](https://github.com/kpreisser/MouseClickSimulator/issues/27)).

## Development

Currently, the implementation contains actions for pressing keys, writing text, SpeedChat, Doodle Interaction Panel and the Automatic Fishing
Function. Furthermore, an action for planting a flower is supported.

The GUI allows to load projects from an XML file. There are some predefined projects included in the **SampleProjects** folder but you can also
create your own XML Simulator Project files. You can use the 
[**Sample Actions.xml**](https://github.com/kpreisser/MouseClickSimulator/blob/master/TTMouseclickSimulator/SampleProjects/Sample%20Actions.xml) as a
template for creating your own project.

When opening a project, the GUI shows the actions which the project contains in a tree-like structure. When the Simulator is running, actions that are 
currently active are marked blue.

In addition to a "Main Action" which usually runs in a loop and can be started by clicking on the generic "Start" button (e.g. the fishing function),
the Mouse Click Simulator supports "Quick Actions" that can be short and non-repeating, e.g. actions to plant specific flowers. For each Quick Action,
a button is created which will start the corresponsing Quick Action when clicking on it.

### Specifying Mouse Coordinates

Currently, mouse coordinates used in the simulator (e.g. in the `scan1` and `scan2` attributes of the `<AutomaticFishing>` element
in the XML files, and in the source code calling `IInteractionProvider.MoveMouse()` or `MouseHelpers.DoSimpleMouseClickAsync()`) are
interpreted for a window with an inner (client area) size of **1600 × 1151** using a 4:3 aspect ratio. These values were kept from the
legacy TT mouse click simulator.

To specify mouse coordinates for Toontown Rewritten, you can do the following to get the resulting coordinates that can be used in the simulator:
- In the Toontown Options, set the display resolution to **800 × 600**. Do **not** resize the window after applying this resolution.
- Determine the (x, y) coordinates within the client area of the window (that is, without the window borders and title bar), e.g. by
  taking a screenshot with F9.
- Calculate the resulting coordinates as follows:
  - x<sub>result</sub> = x ÷ 800 × 1600
  - y<sub>result</sub> = y ÷ 600 × 1151

In the C# code, you have then also specify the alignment (which is needed when the current window aspect ratio is greater than 4:3).
To determine this, resize your Toontown window to **increase the width** (or decrease the height).
- If the element that your coordinates point to stays at the center of the window, specify `VerticalScaleAlignment.Center`, or omit this parameter.
- Otherwise, if the element stays at the left hand side of the window, specify `VerticalScaleAlignment.Left`.
- Otherwise, if the element stays at the right hand side of the window, specify `VerticalScaleAlignment.Right`.

You can find an example for this in [issue #24](https://github.com/kpreisser/MouseClickSimulator/issues/24#issuecomment-306059882).

### TODOs:
- Document how to use the Mouse Click Simulator.
- Document how to create own XML Simulator Project files.
