using Godot;

/// <summary>
/// Optional legacy sound and motorcycle lookups, which stay silent or empty when their files
/// are absent. Character and enemy bodies use SpriteCook resources, not this helper.
/// See <c>docs/STAND_IN_ASSETS.md</c> for the retired downloads' sources and licenses.
/// </summary>
public static class StandInAssets
{
    private const string Sounds = "res://assets/stand_in/kenney_scifi_sounds/";

    /// <summary>Top-down motorcycle facing up the image (Kenney Racing Pack).</summary>
    public const string BikeTexture = "res://assets/stand_in/kenney_racing/motorcycle_blue.png";
    /// <summary>Low engine hum, looped while the electric bike is ridden.</summary>
    public const string BikeEngine = Sounds + "spaceEngineLow_000.ogg";
    /// <summary>Heavy shot for the sentry's locked beam.</summary>
    public const string BeamShot = Sounds + "laserLarge_000.ogg";
    /// <summary>Short shot for each round of the gunner's burst.</summary>
    public const string RoundShot = Sounds + "laserRetro_000.ogg";
    /// <summary>Metal hit for an open-world drone's landed strike.</summary>
    public const string Strike = Sounds + "impactMetal_000.ogg";
    /// <summary>Explosion played when one of these enemies is destroyed.</summary>
    public const string Explosion = Sounds + "explosionCrunch_000.ogg";

    /// <summary>Returns the texture at <paramref name="path"/>, or null when it has been removed.</summary>
    public static Texture2D? Texture(string path) =>
        ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    /// <summary>
    /// Adds a positional player on the SFX bus as a child of <paramref name="owner"/>.
    /// Call from <c>_Ready</c>. Returns null when the sound has been removed.
    /// </summary>
    /// <param name="owner">Node the sound follows.</param>
    /// <param name="path">Resource path of the stream.</param>
    /// <param name="volumeDb">Playback volume.</param>
    /// <param name="loop">True to loop. This sets the flag on the shared cached stream, so use one file per purpose.</param>
    public static AudioStreamPlayer2D? AddPlayer(Node owner, string path, float volumeDb, bool loop = false)
    {
        if (!ResourceLoader.Exists(path)) return null;
        var stream = GD.Load<AudioStream>(path);
        if (stream == null) return null;
        if (loop && stream is AudioStreamOggVorbis ogg) ogg.Loop = true;
        var player = new AudioStreamPlayer2D { Stream = stream, VolumeDb = volumeDb, MaxDistance = 1200f, Bus = "SFX" };
        owner.AddChild(player);
        return player;
    }

    /// <summary>Starts a player if it exists and is in the scene tree; standalone unit tests stay silent.</summary>
    public static void Play(AudioStreamPlayer2D? player)
    {
        if (player != null && player.IsInsideTree()) player.Play();
    }

    /// <summary>
    /// Plays a one-shot sound at <paramref name="anchor"/>'s position so it survives the anchor being freed
    /// in the same frame. The player frees itself when finished. It is added to the scene root, not the
    /// anchor's parent, because encounter code iterates its children and treats each one as an enemy.
    /// </summary>
    public static void PlayDetached(Node2D anchor, string path, float volumeDb)
    {
        if (!anchor.IsInsideTree()) return;
        Node parent = anchor.GetTree().Root;
        if (!ResourceLoader.Exists(path)) return;
        var stream = GD.Load<AudioStream>(path);
        if (stream == null) return;
        var player = new AudioStreamPlayer2D { Stream = stream, VolumeDb = volumeDb, MaxDistance = 1200f, Bus = "SFX" };
        parent.AddChild(player);
        player.GlobalPosition = anchor.GlobalPosition;
        player.Finished += player.QueueFree;
        player.Play();
    }
}
