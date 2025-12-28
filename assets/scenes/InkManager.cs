using Godot;
using System;
using Ink.Runtime;

namespace FrogJumper
{
    public partial class InkManager : Node
    {
        private Story _story;
        
        [Signal]
        public delegate void StoryTextUpdatedEventHandler(string text);
        
        [Signal]
        public delegate void ChoicesPresentedEventHandler(Godot.Collections.Array<string> choices);
        
        [Signal]
        public delegate void StoryEndedEventHandler();

        /// <summary>
        /// Load an Ink story from a JSON file
        /// </summary>
        /// <param name="storyJsonPath">Path to the compiled .json file (e.g., "res://assets/ink_stories/level_complete.json")</param>
        public void LoadStory(string storyJsonPath)
        {
            try
            {
                using var file = FileAccess.Open(storyJsonPath, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    GD.PrintErr($"Failed to open Ink story file: {storyJsonPath}");
                    return;
                }
                
                string jsonContent = file.GetAsText();
                _story = new Story(jsonContent);
                
                GD.Print($"Ink story loaded successfully: {storyJsonPath}");
                
                // Automatically start the story
                ContinueStory();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error loading Ink story: {ex.Message}");
            }
        }

        /// <summary>
        /// Set a variable in the Ink story
        /// </summary>
        public void SetVariable(string variableName, object value)
        {
            if (_story == null)
            {
                GD.PrintErr("No story loaded!");
                return;
            }

            try
            {
                _story.variablesState[variableName] = value;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error setting variable '{variableName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Get a variable from the Ink story
        /// </summary>
        public object GetVariable(string variableName)
        {
            if (_story == null)
            {
                GD.PrintErr("No story loaded!");
                return null;
            }

            try
            {
                return _story.variablesState[variableName];
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error getting variable '{variableName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Continue the story and emit the next text
        /// </summary>
        public void ContinueStory()
        {
            if (_story == null)
            {
                GD.PrintErr("No story loaded!");
                return;
            }

            try
            {
                string fullText = "";
                
                // Continue as long as there's content and no choices
                while (_story.canContinue)
                {
                    string text = _story.Continue();
                    fullText += text;
                }

                // Emit the story text
                if (!string.IsNullOrWhiteSpace(fullText))
                {
                    EmitSignal(SignalName.StoryTextUpdated, fullText.Trim());
                }

                // Check if there are choices
                if (_story.currentChoices.Count > 0)
                {
                    var choices = new Godot.Collections.Array<string>();
                    foreach (var choice in _story.currentChoices)
                    {
                        choices.Add(choice.text);
                    }
                    EmitSignal(SignalName.ChoicesPresented, choices);
                }
                else if (!_story.canContinue)
                {
                    // Story has ended
                    EmitSignal(SignalName.StoryEnded);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error continuing story: {ex.Message}");
            }
        }

        /// <summary>
        /// Choose a choice by index (0-based)
        /// </summary>
        public void MakeChoice(int choiceIndex)
        {
            if (_story == null)
            {
                GD.PrintErr("No story loaded!");
                return;
            }

            try
            {
                if (choiceIndex >= 0 && choiceIndex < _story.currentChoices.Count)
                {
                    _story.ChooseChoiceIndex(choiceIndex);
                    ContinueStory();
                }
                else
                {
                    GD.PrintErr($"Invalid choice index: {choiceIndex}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error making choice: {ex.Message}");
            }
        }

        /// <summary>
        /// Jump to a specific knot in the story
        /// </summary>
        public void JumpToKnot(string knotName)
        {
            if (_story == null)
            {
                GD.PrintErr("No story loaded!");
                return;
            }

            try
            {
                _story.ChoosePathString(knotName);
                ContinueStory();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error jumping to knot '{knotName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Reset the story to the beginning
        /// </summary>
        public void ResetStory()
        {
            if (_story == null)
            {
                GD.PrintErr("No story loaded!");
                return;
            }

            try
            {
                _story.ResetState();
                ContinueStory();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error resetting story: {ex.Message}");
            }
        }
    }
}
