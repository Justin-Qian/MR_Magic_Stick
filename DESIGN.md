## 1. Overview

This project is an MR-based music game. The core mechanic of music games involves players reacting to specific rhythmic beats. For instance, *Beat Saber*, a popular VR music game, guides players to slice blocks with lightsabers at the right moments. This game draws inspiration from *Beat Saber* but introduces innovative gameplay. Users can choose among three modes—Rigid, Elastic, and Pliable—to form a "Magic Stick." The stick's shape is controlled by the distance between two controllers, creating diverse interactive experiences. In the future, the project aims to expand into a multiplayer mode, allowing two players to collaboratively determine the Magic Stick's shape.

**How to play:** After opening the software named "meta01" on Meta Quest 3, follow these steps: click *Level > Level 1*, adjust the height as prompted, and then click *Start Game* to begin.

## 2. Music Visualization

This feature allows cubic blocks to appear in the space at corresponding times while playing music. For the demo, *Little Star* was selected as the soundtrack. Pitch and rhythm data from the music were extracted into a CSV file. At the specified times, note-representing blocks are generated in front of the player (on a spherical surface). Additionally, transparent hint blocks are auto-generated 4 seconds in advance based on the position and timing of the blocks.

## 3. Controller Interaction

This feature enables the formation of different types of Magic Sticks based on the controller positions to collide with the cubic blocks and score points. Using the Meta SDK, the program retrieves the 3D coordinates of the controllers and forms a Magic Stick between them. The stick can take three forms:

1. **Rigid**: The distance between the controllers is fixed.
2. **Elastic**: The stick can stretch or compress, making either the middle segment or the ends effective.
3. **Pliable**: Players can adjust the distance between the controllers to modify the stick's deformation.

The program detects collisions between the stick and the music blocks and provides corresponding feedback.

## 4. UI Design

The UI consists of three parts:

1. **Feedback**: Controlled by the *UI Manager*, it adjusts the content (Perfect, Good, or Missing) and position of the feedback UI. This section also includes *Audio Feedback*, providing corresponding sound cues when players score.
2. **Scores**: Managed by the *ScoreManager*, which uses a singleton pattern to record game data (Score and Combo).
3. **Guidance**: Composed of buttons, it uses the Meta SDK to enable interaction via the controllers.
