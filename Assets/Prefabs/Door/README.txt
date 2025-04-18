# Double Door Entrance Setup

This guide explains how to set up a double door entrance that locks after the player passes through and unlocks when all enemies on the floor are defeated.

## Overview

The EntranceDoubleDoorController system works with the existing DoorController components and FloorManager system to:
1. Allow the player to pass through the entrance initially
2. Close and lock both doors after the player enters 
3. Monitor enemy status on the current floor
4. Automatically unlock and open the doors when all enemies are defeated

## Setup Instructions

1. **Create a Parent Object:**
   - Create an empty GameObject and name it "EntranceDoubleDoor"
   - Position it at the center point between where both doors will be
   - Add the EntranceDoubleDoorController component to it
   - Add a BoxCollider2D component, set it to "Is Trigger", and size it to cover the entrance area

2. **Set Up Left and Right Doors:**
   - Create two door GameObjects as children (or use existing Door prefabs)
   - Position them appropriately on either side of the entrance
   - Make sure each door has a DoorController component
   - Assign these doors to the "leftDoor" and "rightDoor" fields in the EntranceDoubleDoorController

3. **Configure Door Settings:**
   - Adjust the opening/closing direction for each door in the EntranceDoubleDoorController
   - Set appropriate delay values for door closing and opening
   - Set appropriate force values

4. **Audio Setup:**
   - Add an AudioSource component to the parent object
   - Assign appropriate sound clips for closing, locked, and unlock sounds
   
5. **Floor Detection:**
   - Either manually assign the current floor in the inspector
   - Or enable "autoDetectFloor" to automatically find which floor the entrance is on

## Tips

- Make sure the collider on the parent object is large enough to detect when the player passes through
- Adjust the door opening/closing forces and delays to get the desired effect
- If doors aren't behaving correctly, check the door direction settings
- For debugging, check the console for the "Entrance doors locked!" message when the player passes through 