# AuSoCom - Aula Sound Commander

## Implementation Decisions

- The desktop UI is implemented with Avalonia so the same application can run on Windows and macOS.
- The initial implementation targets Windows. The application model and audio facade remain platform-neutral, with ASIO support on Windows.
- NAudio is hidden behind a platform-independent audio facade. A Windows NAudio/ASIO implementation is required for real playback and routing. macOS support will initially preserve the facade and UI structure, but native output is a separate follow-up if CoreAudio integration is not available in NAudio.
- The application is packaged and published as a self-contained single executable for each supported runtime identifier.
- The implementation follows vertical slices: settings and domain model, device discovery and playback facade, scene routing, show navigation, persistence, and then the Avalonia views.
- The source plan and documentation live in `doc/`; source code lives in `src/AuSoCom`; tests live in `test/AuSoComTests`.

## Implementation Checklist

1. Install .NET 10 and create the Avalonia application and MSTest solution structure.
2. Define application-domain entities and JSON persistence with GUID identities.
3. Define the audio facade and implement Windows device discovery and MP3 playback through NAudio/ASIO.
4. Implement scene routing, volume, mute, fade, delay, and show navigation use cases.
5. Build the Avalonia main window, tab content, show controls, scene controls, and modal add/remove selectors.
6. Persist window/tab layout and support open/save/save-as and command-line settings-file loading.
7. Add high-level use-case tests and small technical tests for routing and timing calculations.
8. Publish win-x64 and win-arm64 single-file, self-contained executables.
9. Verify macOS compilation and record the remaining native audio integration requirements.

## Application Elements

Each element has a GUID that is used to reference it in the application and its JSON settings, even when the short name changes

### Inputs

Types of Inputs
- MP3 file, optionally only a portion of a file between a start and end-time (hh:mm:ss.fff) as a stereo channel
- Input device stereo channel
- Input device L and R as mono channels
- The "Stereo Mix" containing the sounds played by other applications

Each found input device is added as both a stereo and two mono inputs to the list of inputs

Each input has a short name the user can provide used for display in other part of the app

### Outputs

- Output device stereo
- Output device L and R as mono channels

Each found output device is added as both a stereo and two mono outputs to the list of outputs

Each output has a short name the user can provide used for display in other part of the app

### Scenes

A scene contains one or more inputs and one or more outputs. 

Each scene has
- a name 
- optional fade-in time
- optional fade-out time
- optional delay (mm:ss.fff) 
- auto-start flag
- a routing matrix, determining what percentage of which input goes to which output. Each input can go into one or more outputs. Each output can have one or more inputs.

New scenes can be created from scratch or by duplicating an existing scene

### Show

There is a single show which is an ordered list of scenes. A scene can occur multiples in a show.

There are back and next buttons to switch between scenes. Switching from a scene stops it (respecting fade-out of the scene). Switching to a scene start  it, if it has the auto-start flag. Without auto-start, one has to press the Start button.

### Settings File

When the first command line parameter to the application is an existing file, it is loaded automatically at start-up.

The JSON settings file contains the above elements and their settings. Additionally, there is a "View" element that stores which are tabs and which are windows, windows (incl. the main window) location and sizes.

## User Interface

Along the top of the main window is a button bar with general application actions.

Files, Input Devices, Output Devices, Scenes and the single show are on separate tabs in a tab control that covers the rest of the window.

Double-clicking a tab header move the content into a separate window and hides the tab. Closing that window moves the content back to the tab. The following specification calls these tabs, even if they are in separate windows.

Along the op of each tab is a button bar for actions.

### General Application Actions

- "Open..." opens a settings file
- "Save" available only when a file has been opened saves the current settings into that file
- "Save as..." opens a file picker and saves the current settings as a JSON settings file

### Files Tab

A table listing the files with short name, full file path, start time, end time

Actions:
- "Add..." opens a file picker of adding one or more files.
- "Remove..." removes all selected files after confirmation
- "Play" plays the currently selected file(s) on the selected device.
- "Play on:" drop-down listing the short names of all available output devices used by the play action

### Input Devices Tab

A table listing the input devices with short name, full device name.

Actions:
- "Refresh" reloads the local machines input devices and adds them as stereo, L and R devices.

### Output Devices Tab

A table listing the output devices with short name, full device name.

Actions:
- "Refresh" reloads the local machines output devices and adds them as stereo, L and R devices.

### Scenes Tab

A table listing all scenes with short name, inputs (comma-delimited short names), outputs (comma-delimited short names) is at the top. Only one can be selected at a time. Below it are controls for the currently selected scene. Between them is a separator for resizing. 

For each input and each output in the scene, there is a vertical slider for control volume and a mute button above it. the button is read when pressed. Inputs are on the left, outputs on the right, with a separator between inputs and outputs.

Further to the right is the routing matrix with inputs across the top and outputs on the left. Each cell in the matrix is a percentage that determines how much of the input goes into the output. Default is 0. 

Actions
- "Add..." shows a window listing short names for all available inputs and outputs that are not already in the selected scene. A search field at the top allows filtering the list.
- "Remove..." shows a window listing all inputs and outputs that are already in the selected scene. A search field at the top allows filtering the list.
- "Duplicate Scene" creates a new scene based on the selected scene

### Show Tab

Actions
- "Add..." shows a window listing short names for all available scenes. A search field at the top allows filtering the list.
- "Remove..." shows a window listing all scenes. A search field at the top allows filtering the list.
- Separator
- "Move up"
- "Move down"
- Separator
- "Back"
- "Start"
- "Next"

### Icons

Use simple, single-color, flat icons alongside the text labels for the following actions:

- Green plus sign for "add"
- Red minus sign for "remove"
- Blue triangle pointing right for "play" and "start"
- Black arrow pointing left for "back"
- Black arrow pointing right for "next"
- Orange arrow pointing up for "move up"
- Orange arrow pointing down for "move down"

## Technical Implementation Details

- Build in C# with .NET 10.
- Create a single executable as the output.
- Must work on Windows and MacOS.
- Source code for the "AuSoCom" project goes into a "src" folder, tests (project called "AuSoComTests") in "test", this plan and any documentation (tbd) in a "doc" folder
- Use the NAudio library.
- Use ASIO for low-latency audio.
- Follow a modular monolith architecture where the class structure follows the business elements described above. Each module should have clearly defined interfaces with the other modules.
- Hide NAudio behind a facade, so replacing it with another audio library is feasible
- Use constructor dependency injection for inversion of control.
- Do not use a DI framework, but create components in the main Progam. and hook them up to each other.
- For unit tests use MSTest2, Moq and Fluent Assertions.
- Follow the Chicago School of Testing by focusing on high-level tests of entire use-cases. 
- Use mockist kinds of unit tests only for very small technical components to cover all possible combinations of parameters.
