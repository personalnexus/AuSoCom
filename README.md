# AuSoCom — Aula Sound Commander

AuSoCom (Aula Sound Commander) is a desktop application for organizing audio files and input/output devices into reusable audio scenes, then arranging scenes into a show. A scene defines which sources are routed to which outputs, along with per-input gain, volume, mute, timing, and routing levels.

## Application Menu

- **Open...** — opens AuSoCom show files and provides a dropdown of recently opened or saved show files. The recent-file list is stored in `AuSoCom.appsettings.json` beside the executable.

- **Save** — saves the show into the current file, or into a new file (like save as) when there is no current file.

- **Save as...** — saves the show into a new file which then becomes the current file.


## Tabs

- **Files** — Add audio files, edit their short names and playback start/end times. Selecting one file opens the preview player, where you can play, pause, stop, seek, and set its start or end time at the current position.

- **Input Devices** — View and rename discovered input devices. Mark devices as favorites, so they appear at the top of the device list or have them automatically included in new scenes.

- **Output Devices** — View and rename discovered output devices, with the same favorite and auto-add options.

- **Scenes** — Create or duplicate scenes and manage each scene’s file/device inputs, outputs, and auto-start setting. Adjust per-input delay, fade-in, and fade-out times below each fader, plus input/output gain, volume, mute, and routing.

- **Show** — Build an ordered show from existing scenes, reorder or navigate between them, and start or stop scene playback. The selected scene’s controls are available alongside the show list.

Live scene audio and device routing are currently supported on Windows only.