# Robotini-Racing-Simulator

Simulator for training machine vision racing AIs, created with Unity.

Clone this repo and open it in Unity. Run and enjoy!

At start, you can control the car with arrow keys. But this is not the point.

The point is to connect your AI to localhost:11000. The simulator will
feed you PNG images from the car's front camera. Like this

1. two bytes indicating the size of the next image (higher byte first)
2. PNG image data
3. goto 1

The simulator accepts control commands as a JSON string terminated with \n from the AI:

- `{ "action": "forward", "value": 0.5}\n` where value is the throttle value between 0 and 1.
- `{ "action": "turn", "value": -0.1}\n` where value is the direction for the front wheels. 1.0 is full left and -1.0 is full right. 

Enjoy!

# Spectator socket

The simulator serves JSON car positions on port 11001.

# Playback mode and WebGL

WIP!

You can build a WebGL build with Unity. It results into an index.html file containing the player. You need to server it over HTTP though - file protocol not supported.

When running in WebGL, the simulator goes to playback mode. The idea is to be able to play back earlier races in a Web UI. Currently it doesn't have any playback capabilities, just initial testing that we can launch it in WebGL, detect the WebGL environment and skip opening any TCP server sockets. Next step would be to download a recorded race and play it.