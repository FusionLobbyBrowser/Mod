# FLB

This mod enables the [Fusion Lobby Browser website](https://fusion.hahoos.dev) to join a Fusion lobby.

It utilizes a couple of ways of connection:

1. **URI Scheme** - The mod creates a custom URI scheme which makes it possible for the website to contact a Bridge executable (explained below)
2. **Bridge Executable** - The mod creates a URI scheme for a imported executable by the mod, which serves as a middle man between the browser and game. This replaces the old logic, which relied on contacting a local http server which was blocked by the browser most of the time.
3. **HTTP Server** - The mod starts an HTTP server that allows for joining fusion lobbies when the game is already loaded.

## Bridge Configuration

The Bridge allows for some configuration on how it's supposed to work. This can be done by modifying the `config.json` file located in `Game Directory > UserData > FLB`.

If you do NOT see the file, you must launch the game with the website at least once.

### ExitTime (default: 5)

The time it takes for the console to close after everything is done.

### HideConsole (default: true)

Whether the console should be hidden by default or not.

### LaunchWithSteam (default: true)

Should BONELAB be launched with Steam (allowing for Overlay/Recording) or not

### NonSteamAppID (default: "-1")

The App ID of the Meta Horizon Link version of the game. To get the value needed, refer to the tutorial found below.

## Launching with Steam (Meta Horizon Link version of BONELAB)

Unfortunately, Steam does not have a straightforward way of launching a non-steam game with the overlay & other things. This forced me to make workarounds (for example having to utilize yet another HTTP Server to communicate between the bridge & the game).

To setup, you need to configure the `NonSteamAppID` option. To do so:

* Create a shortcut (if you don't have one already) for BONELAB by going to the Steam application, on top the `Games` tab and `Add a Non-Steam Game to My Library`. There select the executable of BONELAB (should be `BONELAB_Oculus_Windows64.exe`).
  * If you don't know the location, go to the Meta Horizon Link app. Once you're there go the Library and press the 3 dots on BONELAB. Next go to Details and copy the location provided there. This is the location where the executable is located. You can simply paste it into the location bar (fuck you Tarek) in File Explorer.
* Next, you need to have a desktop shortcut file. If you haven't created one already, right-click the shortcut in the Steam Library, go to Manage and press `Add desktop shortcut`.
* Locate the file on your Desktop and open the contents with Notepad or another text editor.
* Look for a URL parameter (which should look like `URL=steam://rungameid/10844567662499987456`). When you've done so, copy the numbers and paste it into the configuration file (NOTE: you **MUST** put it in "", so basically: `"nonSteamAppId": "10844567662499987456"`).
* And you're done!
