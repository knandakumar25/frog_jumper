# Implementation Plan: AXIOM-7 Voice Recordings in Story Intro

## Overview
This plan adds voice recording support to the `story_intro.gd` scene, playing synchronized audio clips for each of the 9 dialogue lines spoken by AXIOM-7.

## Requirements
1. Voice recordings: `assets/speech/AXIOM-7/AXIOM-7-1 (ENG).mp3` to `AXIOM-7-9 (ENG).mp3`.
2. Synchronize audio with the 9 dialogue lines.
3. Stop current audio and start next on `continue_button` press (when moving to the next line).
4. Stop all audio immediately on `skip_button` press.
5. Use `AudioStreamPlayer` consistent with project patterns.

## Technical Design

### 1. Audio Player Setup
To ensure the implementation is self-contained and consistent with the `Global` audio player pattern, an `AudioStreamPlayer` will be created programmatically in the `_ready()` function.

- **Variable**: `var voice_player: AudioStreamPlayer`
- **Initialization**:
  ```gdscript
  voice_player = AudioStreamPlayer.new()
  add_child(voice_player)
  ```

### 2. Audio Synchronization Logic
The `_start_next_line()` function already manages the `current_line_index`. We will leverage this index to dynamically load the corresponding audio file.

- **Audio Path Construction**:
  The voice files are 1-indexed, while `current_line_index` is 0-indexed.
  Path template: `"res://assets/speech/AXIOM-7/AXIOM-7-%d (ENG).mp3" % (current_line_index + 1)`

- **Playback Trigger**:
  Inside `_start_next_line()`, we will load the stream and call `.play()`.

### 3. Control Flow Integration

- **Moving to Next Line**: 
  When `_on_continue_button_pressed()` increments `current_line_index` and calls `_start_next_line()`, the `voice_player.play()` call will automatically stop the previous audio and start the new one.
- **Skipping Typewriter**: 
  If the user clicks continue while `is_typing` is true, only the text is updated. The audio will continue playing uninterrupted.
- **Skipping Tutorial**: 
  `_on_skip_button_pressed()` calls `_proceed_to_level_selection()`. We will add `voice_player.stop()` here to kill all audio immediately.

## Step-by-Step Implementation

### Step 1: Declare Voice Player
Add `var voice_player: AudioStreamPlayer` to the member variables.

### Step 2: Initialize Voice Player
In `_ready()`, create the `AudioStreamPlayer` and add it as a child.

### Step 3: Implement Audio Playback in `_start_next_line()`
Modify `_start_next_line()` to:
1. Calculate the file path based on `current_line_index + 1`.
2. Check if the resource exists using `ResourceLoader.exists()`.
3. Assign the loaded stream to `voice_player.stream`.
4. Call `voice_player.play()`.

### Step 4: Implement Audio Stopping on Exit
Modify `_proceed_to_level_selection()` to call `voice_player.stop()`.

## Critical Files for Implementation
- `assets/scenes/story_intro.gd`
