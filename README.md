# Robotini-Racing-Simulator

Simulator for training machine vision racing AIs, created with Unity.

Clone this repo and open it in Unity. Run and enjoy!

## The bot connection

The point is to connect your AI to localhost:11000. The simulator will
feed you PNG images from the car's front camera. Like this

1. two bytes indicating the size of the next image (higher byte first)
2. PNG image data
3. goto 1

The simulator accepts control commands as a JSON string terminated with \n from the AI:

- `{ "action": "forward", "value": 0.5}\n` where value is the throttle value between 0 and 1.
- `{ "action": "turn", "value": -0.1}\n` where value is the direction for the front wheels. 1.0 is full left and -1.0 is full right. 

## RaceParameters

The RaceParameters.json file can be used to control the operating mode of the Simulator. First of all, `mode` can be one of `development`, `race` or `playback` for different modes.

In `development` mode the simulator accepts bot connections and places cars immediately on the track for free practice with lap timing

In `race` mode the simulator accepts bot connections while in the "race lobby" phase. Then it starts Qualifying and finally the Race. You can move tho the next phase using SPACEBAR. 
It also moves automatically forward based on RaceParameters, see self-documenting code in [RaceParameters.cs](Assets/Scripts/RaceParameters.cs).

In `playback` mode the simulators plays back a recorded race log. When running in WebGL, the playback mode is always assumed. See below.

# Playback mode and WebGL

You can build a WebGL build with Unity. It results into an index.html file containing the player. You need to server it over HTTP though - file protocol is not supported.

At the moment it always looks for race data at http://localhost:8000/race-capture.json and this should be parameterized soon.

When running a race, the simulator saves race data to `race.log`. If you serve the resulting file over HTTP to the player, you should see a race.

## Spectator socket

The simulator serves a full capture of race events on port 11001 as a newline-separated JSON stream. This could be used for watching the race live.

You can also send UICommands through the socket, such as:

    {"type":"ProceedToNextPhase"}