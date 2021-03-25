# Robotini-Racing-Simulator

Simulator for training machine vision racing AIs, created with Unity.

Clone this repo and open it in Unity. Select the "Track" scene on the editor (Project -> Scenes -> Track) and start the simulator using the Play button. You should see the race track and the message "Free practice".

## The bot connection

The point is to connect your AI to localhost:11000. The simulator will feed you PNG images from the car's front camera.

Your bot should do the following:

1. Connect to simulator:11000
2. Send a "login" JSON message: `{"name":"your bot name", "color": "team color e.g. #ff0000", "teamId": "your team ID"}` followed by `\n`.
3. Read a frame: 2 bytes indicating the size of image, followed by PNG image data
4. Respond with 1 or more commands (see below)
5. goto 3

The simulator accepts control commands as a JSON string terminated with `\n` from the bot:

- `{ "action": "forward", "value": 0.5}\n` where value is the throttle value between 0 and 1.
- `{ "action": "turn", "value": -0.1}\n` where value is the direction for the front wheels. 1.0 is full left and -1.0 is full right. 

Prepare to process 60 frames per second using a single thread.

Remember to use the TCP_NODELAY option in your bot TCP socket to ensure that the commands are sent without delay.

## RaceParameters

The RaceParameters.json file can be used to control the operating mode of the Simulator. First of all, `mode` can be one of `development`, `race` or `playback` for different modes.

In `development` mode the simulator accepts bot connections and places cars immediately on the track for free practice with lap timing

In `race` mode the simulator accepts bot connections while in the "race lobby" phase. Then it starts Qualifying and finally the Race. You can move tho the next phase using SPACEBAR. 
It also moves automatically forward based on RaceParameters, see self-documenting code in [RaceParameters.cs](Assets/Scripts/RaceParameters.cs).

In `playback` mode the simulators plays back a recorded race log. When running in WebGL, the playback mode is always assumed. See below.

## Playback

The simulator saves all race events into the file `race.log`, or a file specified by the `raceLogFile` property in RaceParameters.json.

### Playback in Unity Editor

This file can be played back by running the simulator in `playback` mode (set in RaceParameters.json). When playing back in Unity Editor, it will use the file specified in RaceParameters (defaults to `race.log`).

### Playback with WebGL

You can build a WebGL build with Unity. It results into an index.html file containing the player. You need to server it over HTTP though - file protocol is not supported. The web build can only be used to playback earlierly recorded `race.log` files. At the moment it always looks for race data at http://localhost:8000/race.log.

### Building the WebGL Player

To build and play with WebGL:

1. In build settings, choose WebGL build
2. In player settings disable compression (otherwise won't work, at the moment)
3. Build it to `./robotini-web-player`

### Try the WebGL player locally

1. Copy a `race.log` file to `./robotini-web-player`
2. Start a HTTP server in `./robotini-web-player`, serving the files on port 8000. 
3. Open http://localhost:8000

### Publishing the WebGL player

To publish the latest WebGL player build it first and the run `make publish-web-player`. This will require AWS permissions.

## Spectator socket

The simulator serves a full capture of race events on port 11001 as a newline-separated JSON stream. This could be used for watching the race live.

You can also send UICommands through the socket, such as:

    {"type":"ProceedToNextPhase"}

## Command-line build

To build on the command-line, Unity editor must be closed.

    make linux-player