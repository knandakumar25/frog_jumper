using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>Shared rendering of original later-campaign plates and the established illustrated operative sheets.
/// Environment layers remain decorative; gameplay hurtboxes and platforms stay authoritative.</summary>
public static class CampaignArtwork
{
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static readonly Dictionary<string, List<(float Start, float End, string Speaker)>> Cues = new();
    private static readonly string[] Ids = { "surya_prime", "oceanus", "cyber_shogun", "phoenicia", "mirage_blade", "apex_titan" };
    private static readonly string[] Names = { "SURYA", "OCEANUS", "KAITO", "AURA", "MIRAGE", "TITAN" };
    private static readonly Color[] Colors = { new("ffe29a"), new("66d9e8"), new("f87171"), new("f472b6"), new("34d399"), new("c084fc") };
    private static readonly float[] Positions = { 470, 665, 860, 1055, 1250, 1450 };

    /// <summary>Resource root for one of the seven reviewed environment families.</summary>
    public static string FamilyPath(string family) => $"res://assets/environments/later_campaign/{family}/";

    /// <summary>Checks essential art before presentation; missing speech remains a separate timed-subtitle fallback.</summary>
    public static bool Available(string family, string artPath) =>
        FileAccess.FileExists(artPath.TrimEnd('/') + "/cues.json") &&
        ResourceLoader.Exists(FamilyPath(family) + "environment_structure.png") &&
        ResourceLoader.Exists(FamilyPath(family) + "aftermath_structure.png");

    /// <summary>Loads each validated texture once; no synchronous image decoding is repeated per frame.</summary>
    private static Texture2D? Texture(string path)
    {
        if (Textures.TryGetValue(path, out var texture)) return texture;
        if (!ResourceLoader.Exists(path)) return null;
        texture = GD.Load<Texture2D>(path);
        if (texture != null) Textures[path] = texture;
        return texture;
    }

    /// <summary>Draws independent base/architecture/emissive plates in the caller's existing camera transform.</summary>
    public static void DrawEnvironment(CanvasItem canvas, string family, float seconds, bool aftermath = false, bool drawBase = true)
    {
        string prefix = aftermath ? "aftermath" : "environment";
        foreach (string layer in new[] { "base", "structure", "lights" })
        {
            if (layer == "base" && !drawBase) continue;
            var texture = Texture(FamilyPath(family) + prefix + "_" + layer + ".png");
            if (texture == null) continue;
            float alpha = layer == "lights" ? .86f + Mathf.Sin(seconds * 1.4f) * .12f : 1;
            canvas.DrawTextureRect(texture, new Rect2(0, 0, 1920, 1080), false, new Color(1, 1, 1, alpha));
        }
    }

    /// <summary>Resolves the current authored speaker and cue age without changing the shot schedule or media timeline.</summary>
    private static (string Speaker, float Age) SpeakerAt(string artPath, float seconds)
    {
        string path = artPath.TrimEnd('/') + "/cues.json";
        if (!Cues.TryGetValue(path, out var cues))
        {
            cues = new();
            if (FileAccess.FileExists(path))
            {
                using var document = JsonDocument.Parse(FileAccess.GetFileAsString(path));
                foreach (var cue in document.RootElement.EnumerateArray())
                    cues.Add((cue.GetProperty("start").GetSingle(), cue.GetProperty("end").GetSingle(),
                        cue.GetProperty("speaker").GetString()?.ToUpperInvariant() ?? ""));
            }
            Cues[path] = cues;
        }
        foreach (var cue in cues) if (seconds >= cue.Start && seconds < cue.End) return (cue.Speaker, seconds - cue.Start);
        return ("", 0);
    }

    /// <summary>Moves dialogue shots toward the recognizable speaker, preserving a smooth transition from existing camera motion.</summary>
    public static Vector2 Focus(string artPath, float seconds, Vector2 authored)
    {
        var cue = SpeakerAt(artPath, seconds);
        int speaker = Array.FindIndex(Names, name => cue.Speaker.StartsWith(name, StringComparison.Ordinal));
        float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp(cue.Age / 1.2f, 0, 1));
        // Off-screen command narration establishes both setting and the complete squad, avoiding low cropped feet.
        if (cue.Speaker.StartsWith("AXIOM", StringComparison.Ordinal))
            return authored.Lerp(new Vector2(960, 560), blend);
        if (cue.Speaker.StartsWith("VOID COMMANDER", StringComparison.Ordinal))
            return authored.Lerp(new Vector2(960, 450), blend);
        if (speaker < 0) return authored;
        return authored.Lerp(new Vector2(Positions[speaker], 575), blend);
    }

    /// <summary>Draws the established six-operative squad at readable native scale, including armor, equipment and speaker beacons.</summary>
    public static void DrawSquad(CanvasItem canvas, string artPath, float seconds)
    {
        var cue = SpeakerAt(artPath, seconds);
        for (int i = 0; i < Ids.Length; i++)
        {
            bool speaking = cue.Speaker.StartsWith(Names[i], StringComparison.Ordinal);
            Vector2 feet = new(Positions[i], 765 + Mathf.Sin(seconds * 1.6f + i) * 1.2f);
            float height = i is 0 or 5 ? 255 : 235;
            DrawOperative(canvas, Ids[i], feet, height, speaking);
            canvas.DrawArc(feet + new Vector2(0, 6), speaking ? 51 : 32, 0, Mathf.Tau, 32,
                new Color(Colors[i], speaking ? .85f : .25f), speaking ? 3 : 1, true);
            if (speaking) canvas.DrawString(ThemeDB.FallbackFont, feet + new Vector2(-60, -height - 24),
                Names[i], fontSize: 20, modulate: Colors[i]);
        }
    }

    /// <summary>
    /// Draws one squad member from their SpriteCook atlas (<c>assets/characters/rendered/&lt;id&gt;_atlas.png</c>),
    /// or nothing while that atlas is not made yet (an intentional gap). Each 256x256 atlas cell puts
    /// the feet on y=224 and centres the body, so the cell is scaled to <paramref name="height"/> over
    /// that baseline and placed with its baseline on <paramref name="feet"/>. A speaker uses the
    /// second idle frame so the pose changes slightly while they talk.
    /// </summary>
    private static void DrawOperative(CanvasItem canvas, string id, Vector2 feet, float height, bool speaking)
    {
        var texture = Texture($"res://assets/characters/rendered/{id}_atlas.png");
        if (texture == null) return;
        const float cell = 256, baseline = 224;
        float scale = height / baseline;
        var region = new Rect2(speaking ? cell : 0, 0, cell, cell);
        canvas.DrawTextureRectRegion(texture, new Rect2(feet.X - cell / 2 * scale, feet.Y - baseline * scale,
            cell * scale, cell * scale), region);
    }

    /// <summary>Adds the matching rendered boss armor and emissive plate without changing collision or phase effects.</summary>
    public static void AttachBoss(Node2D boss, string family)
    {
        foreach (string layer in new[] { "structure", "lights" })
        {
            var texture = Texture(FamilyPath(family) + "boss_" + layer + ".png");
            if (texture == null) continue;
            boss.AddChild(new Sprite2D { Name = "Armor_" + layer, Texture = texture,
                Scale = Vector2.One * .45f, Position = new Vector2(0, -7), ZIndex = 2 });
        }
    }

    /// <summary>Uses the same identifiable armor plate for a cinematic commander and its gameplay encounter.</summary>
    public static void DrawCommander(CanvasItem canvas, Vector2 feet)
    {
        foreach (string layer in new[] { "structure", "lights" })
        {
            var texture = Texture(FamilyPath("judgment_day") + "boss_" + layer + ".png");
            if (texture != null) canvas.DrawTextureRect(texture, new Rect2(feet + new Vector2(-130, -225), new Vector2(260, 260)), false);
        }
    }
}
