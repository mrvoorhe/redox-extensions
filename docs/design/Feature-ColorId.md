I want you to implement a new feature that will display the color information for an object when I ID the object.  The feature should be off by default.

The follow commands are ways to turn it on
`/re showcolors` - Toggle mode, flips to opposite state
`/re showcolors on/off` - explicit on/off 

Display a message in the chat saying the state after the command is received.

It looks like the information you need is in docs/ref/decal/game-events.md under the "Identify Object" event.

Color information should be written to the chat output.