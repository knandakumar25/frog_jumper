using Godot;

/// <summary>Compatibility node; the prompt autoload installs defaults before saved bindings.</summary>
public partial class InputBootstrap : Node
{
    /// <summary>Retains the scene node without rebuilding any remapped keys.</summary>
    public override void _Ready() => InputActionDefaults.Initialize();
}
