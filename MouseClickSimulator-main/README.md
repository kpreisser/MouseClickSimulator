# Mouse Click Simulator for Toontown Rewritten and Corporate Clash

This is a new implementation of the older **"TT-Mausklick"** simulator, intended to work with Toontown Rewritten and Corporate Clash.
It is implemented in C# (.NET 8.0) and runs on Windows.

The TT Mouse Click Simulator is able to automatically fish in specific locations like Punchline Place, TTC. To accomplish this, it scans the
screen to detect fish bubbles, calculates how far the rod must be cast to catch the fish, and moves the Toon to the fisherman to sell the fish.

Additionally, for Toontown Rewritten, the simulator can plant and water flowers using 1 to 8 jellybean combinations, which is implemented using "Quick Actions".
By clicking on a button with the flower's name, the Mouse Click Simulator will plant the flower by selecting the correct jellybean combination, and then water it.

You can watch a video of the <a href="https://www.youtube.com/watch?v=uq7VaJkO6-k" target="_blank">Automatic Fishing Function for Tenor Terrace</a>
and <a href="https://www.youtube.com/watch?v=dS-gBcvsjz4" target="_blank">Punchline Place</a>.

![](https://user-images.githubusercontent.com/13289184/150864161-64f25257-9175-48a5-b713-2634dbf2aaaa.png)

Note: This Simulator does not inject code into or otherwise manipulate the game. It only interacts with TT by taking screenshots to analyze the window content
(for the fishing action) and simulating mouse clicks/movements and pressing keys.

When enabling **Background Mode**, the simulator directly sends mouse and keyboard inputs to the Toontown window (instead of simulating gobal inputs),
so you can do other work while the simulator is running.

## WARNING

Use this program at your own risk!
Toontown Rewritten and Corporate Clash state in their Terms of Use that you should not use automation software, so you might risk a ban if you use this program.

## Running the Mouse Click Simulator

Please see the topic [Running the Simulator](https://github.com/kpreisser/MouseClickSimulator/wiki/Running-the-Simulator) for guidance how to download,
build and run the Mouse Click Simulator on your computer.

## Release Notes

**2023-11-16** (Commit [4ed3512](https://github.com/kpreisser/MouseClickSimulator/commit/4ed3512e139c233175371765f594d192a25f8c04))
- Updated to **.NET 8.0**.
  Due to this change, if you had already installed the .NET SDK 6.0 or 7.0, you will need to install the
  [.NET SDK 8.0 or higher](https://dotnet.microsoft.com/download) in order to be able to build the application.

**2022-01-24** (Commit [db8435c](https://github.com/kpreisser/MouseClickSimulator/commit/db8435c5d0c7309c3e09b214b0a587b3e12d32c2))

- Added support for alphabetic keys in the `KeyPress` action, and added new `KeyDown` and `KeyUp` actions.
- Added an option to convert arrow keys into WASD keys for movement ([#40](https://github.com/kpreisser/MouseClickSimulator/issues/40)).

**2022-01-23** (Commit [e8cb4a8](https://github.com/kpreisser/MouseClickSimulator/commit/e8cb4a8e083d31bfd81d9c88405d5177007b5183))

- Added support for **Corporate Clash** ([#32](https://github.com/kpreisser/MouseClickSimulator/issues/32)).
- Background mode is now disabled by default (like in earlier versions that didn't support this mode) as it doesn't seem to always work 
  correctly.
- Fixed an issue for auto fishing in Walrus Way that occured often when using a low FPS rate (like 30), where the toon would miss the 
  fishing board after selling the fish.

**2022-01-15** (Commit [834a4ca](https://github.com/kpreisser/MouseClickSimulator/commit/834a4ca019d7f393c7bf124433c391e7f1d63ebd))

- Improved the gardening projects to automatically water a flower after planting it.
- Improved the runtime behavior by checking requirements according to the declared capabilities of actions.

**2022-01-10** (Commit [bcec185](https://github.com/kpreisser/MouseClickSimulator/commit/bcec1855a60b4a2919545046691c655c31b0cc8c))

- Updated to **.NET 6.0**.
  Due to this change, the prerequisites for building the simulator have slightly changed. Please see the topic 
  [Running the Simulator](https://github.com/kpreisser/MouseClickSimulator/wiki/Running-the-Simulator) for the new instructions.
- Added **Background Mode** which directly sends mouse and keyboard inputs to the Toontown window (instead of simulating gobal inputs).
  This allows you to do other work while the simulator is running.
- Added support for the 64-bit version of TT Rewritten ([#37](https://github.com/kpreisser/MouseClickSimulator/issues/37)).
- Added a new project for keeping the toon awake.
- Improved key press durations in the fishing projects, and added new projects for use during the winter theme.
- Fixed a bug that prevented the window location from being detected correctly when using multiple monitors with different DPI settings.

<details>
  <summary>Click to expand older release notes</summary>

**2018-08-12** (Commit [67c4a87](https://github.com/kpreisser/MouseClickSimulator/commit/67c4a87c1db7fd906f3dfc88aa6cc26c51dc6d4f))

- The simulator now detects when multiple TT Rewritten windows are open, and allows to select the one that should be used
  ([#27](https://github.com/kpreisser/MouseClickSimulator/issues/27)).

</details>

## Development

Currently, the implementation contains actions for pressing keys, writing text, SpeedChat, Doodle Interaction Panel and the Automatic Fishing
Function. Furthermore, an action for planting a flower is supported.

The GUI allows to load projects from an XML file. There are some predefined projects included in the **SampleProjects** folder but you can also
create your own XML Simulator Project files. You can use the 
[**Sample Actions.xml**](https://github.com/kpreisser/MouseClickSimulator/blob/main/TTMouseclickSimulator/SampleProjects/ToontownRewritten/Sample%20Actions.xml) as a
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

To specify mouse coordinates for Toontown, you can do the following to get the resulting coordinates that can be used in the simulator:
- In the Toontown Options, for **Toontown Rewritten**, set the display resolution to **800 × 600**. Do **not** resize the window after applying this resolution.
  Or, for **Corporate Clash**, set the display resolution to any resolution you prefer, and set the aspect ratio to **4:3**.
- Determine the (x, y) coordinates within the client area of the window (that is, without the window borders and title bar), e.g. by
  taking a screenshot with F9.
- Calculate the resulting coordinates as follows (replace 800 and 600 with the actual display resolution that you have set before):
  - x<sub>result</sub> = x ÷ 800 × 1600
  - y<sub>result</sub> = y ÷ 600 × 1151

In the C# code, you have then also specify the horizontal alignment (which is needed when the current window aspect ratio is greater than 4:3).
To determine this, for **Toontown Rewritten**, resize your Toontown window to **increase the width** (or decrease the height).
Or, for **Corporate Clash**, reset the aspect ratio to **Adaptive**.
- If the element that your coordinates point to stays at the center of the window, specify `HorizontalScaleAlignment.Center`, or omit this parameter.
- Otherwise, if the element stays at the left hand side of the window, specify `HorizontalScaleAlignment.Left`.
- Otherwise, if the element stays at the right hand side of the window, specify `HorizontalScaleAlignment.Right`.

You can find an example for this in [issue #24](https://github.com/kpreisser/MouseClickSimulator/issues/24#issuecomment-306059882).

### TODOs:
- Document how to use the Mouse Click Simulator.
- Document how to create own XML Simulator Project files.
