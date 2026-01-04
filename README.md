# FLB

This mod enables the [Fusion Lobby Browser website](https://fusion.hahoos.dev) to join a Fusion lobby.

It utilizes two ways of connection:
1. **URI Scheme** - The mod creates a custom URI scheme which makes it possible for the website to open the game
2. **HTTP Server** - The mod starts an HTTP server that allows for joining fusion lobbies when the game is already loaded.

This may sound alarming to some and I can understand that. Some browsers will not allow the second method, they will either show a pop up asking if you allow or you will need to turn off something.
