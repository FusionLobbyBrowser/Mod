# FLB

This mod enables the [Fusion Lobby Browser website](https://fusion.hahoos.dev) to join a Fusion lobby.

It utilizes a couple of ways of connection:
1. **URI Scheme** - The mod creates a custom URI scheme which makes it possible for the website to contact a Bridge executable (explained below)
2. **Bridge Executable** - The mod creates a URI scheme for a imported executable by the mod, which serves as a middle man between the browser and game. This replaces the old logic, which relied on contacting a local http server which was blocked by the browser most of the time.
3. **HTTP Server** - The mod starts an HTTP server that allows for joining fusion lobbies when the game is already loaded. This is **ONLY** used by the Bridge
