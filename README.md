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