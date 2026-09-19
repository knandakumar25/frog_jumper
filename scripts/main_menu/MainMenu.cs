using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control
{
    // ... (rest of class unchanged) ...
    private void Setup3DTitle()
    {
        // Title is now rendered in the 3D menu scene; hide the 2D placeholder.
        if (_titleContainer != null)
        {
            var oldTitle = _titleContainer.GetNodeOrNull<Label>("MainTitle");
            oldTitle?.QueueFree();
            _titleContainer.Visible = false;
        }
        // No additional 3D viewport setup needed here.
    }
    // ... (rest of class unchanged) ...
}