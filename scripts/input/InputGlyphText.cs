using Godot;
using System.Text;

/// <summary>
/// Carries controller button images through ordinary prompt strings and draws them in
/// <see cref="RichTextLabel"/>s.
///
/// <para>Prompt text is built by joining translated words with bindings, so an image has to travel
/// inside a plain string. <see cref="Mark"/> wraps an image path and its text label in three
/// private-use characters. A marked string must only reach a label through <see cref="Set"/>, which
/// turns each marker into an inline <c>[img]</c> and escapes everything else, or through
/// <see cref="ToPlain"/>, which keeps the text label. Writing a marked string straight into a
/// <see cref="Label"/> would show the marker characters, so callers ask for marked bindings only
/// where the destination is a rich label set up here.</para>
/// </summary>
public static class InputGlyphText
{
    /// <summary>Private-use characters that open, split and close one image marker.</summary>
    private const char Open = '', Split = '', Close = '';

    /// <summary>Meta key holding the plain (image-free) text of a rich prompt label, for tests and accessibility.</summary>
    public const string PlainMeta = "input_prompt_plain";

    /// <summary>Builds one image marker. <paramref name="label"/> is the text shown when images are not drawn.</summary>
    public static string Mark(string path, string label) => $"{Open}{path}{Split}{label}{Close}";

    /// <summary>True when the text holds at least one image marker.</summary>
    public static bool HasImage(string text) => text.IndexOf(Open) >= 0;

    /// <summary>Replaces every marker with its text label; text without markers is returned unchanged.</summary>
    public static string ToPlain(string text) => Convert(text, 0, rich: false);

    /// <summary>
    /// Converts marked text to BBCode: markers become <c>[img height=H]path[/img]</c> and every other
    /// character is escaped, so translated text containing brackets is never read as a tag.
    /// </summary>
    /// <param name="text">Marked or plain text.</param>
    /// <param name="height">Image height in pixels, usually a little above the font size.</param>
    public static string ToBbcode(string text, int height) => Convert(text, height, rich: true);

    /// <summary>Walks the string once, copying text and expanding markers either to images or to labels.</summary>
    private static string Convert(string text, int height, bool rich)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var output = new StringBuilder(text.Length + 32);
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            int split = c == Open ? text.IndexOf(Split, i + 1) : -1;
            int close = split > 0 ? text.IndexOf(Close, split + 1) : -1;
            if (close > 0)
            {
                string path = text.Substring(i + 1, split - i - 1);
                string label = text.Substring(split + 1, close - split - 1);
                if (rich) output.Append("[img height=").Append(height).Append(']').Append(path).Append("[/img]");
                else output.Append(label);
                i = close + 1;
                continue;
            }
            // A stray marker character (never produced by Mark) is dropped rather than shown.
            if (c is Open or Split or Close) { i++; continue; }
            if (rich && c == '[') output.Append("[lb]");
            else if (rich && c == ']') output.Append("[rb]");
            else output.Append(c);
            i++;
        }
        return output.ToString();
    }

    /// <summary>
    /// Creates a prompt label that can show button images: BBCode on, sized to its content, no scrolling,
    /// word wrapping and mouse-transparent, with the given font size, colour and optional font.
    /// </summary>
    public static RichTextLabel CreateLabel(int fontSize, Color color, Font? font = null)
    {
        var label = new RichTextLabel
        {
            BbcodeEnabled = true, FitContent = true, ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled,
            // The 128 px button images are shrunk to text height; mipmaps keep them smooth.
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
        };
        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
        label.AddThemeColorOverride("default_color", color);
        if (font != null) label.AddThemeFontOverride("normal_font", font);
        return label;
    }

    /// <summary>
    /// Shows marked text in a rich label. Images are sized from the label's font size, and the plain
    /// version is kept in the <see cref="PlainMeta"/> meta for <see cref="Plain"/>.
    /// </summary>
    public static void Set(RichTextLabel label, string text)
    {
        if (!GodotObject.IsInstanceValid(label)) return;
        int height = Mathf.RoundToInt(Mathf.Max(12, label.GetThemeFontSize("normal_font_size")) * 1.35f);
        label.SetMeta(PlainMeta, ToPlain(text));
        label.Text = ToBbcode(text, height);
    }

    /// <summary>Reads what a prompt control says, without images: a rich label's saved plain text or a Label's text.</summary>
    public static string Plain(Control? control) => control switch
    {
        RichTextLabel rich when rich.HasMeta(PlainMeta) => rich.GetMeta(PlainMeta).AsString(),
        RichTextLabel rich => rich.GetParsedText(),
        Label label => label.Text,
        Button button when button.HasMeta(PlainMeta) && button.Text.Length == 0 => button.GetMeta(PlainMeta).AsString(),
        Button button => button.Text,
        _ => "",
    };

    /// <summary>
    /// Replaces a scene-authored <see cref="Label"/> with an equivalent rich label in the same place:
    /// same name, parent position, anchors, offsets, size flags, minimum size, visibility, alignment,
    /// wrapping, font, font size, colour and outline. The old label is freed, so callers must keep
    /// the returned node instead. A new rich label is always created.
    /// </summary>
    public static RichTextLabel Upgrade(Label label)
    {
        var rich = CreateLabel(label.GetThemeFontSize("font_size"), label.GetThemeColor("font_color"), label.GetThemeFont("font"));
        rich.Name = label.Name;
        rich.Visible = label.Visible;
        rich.Modulate = label.Modulate;
        rich.SelfModulate = label.SelfModulate;
        rich.ZIndex = label.ZIndex;
        rich.HorizontalAlignment = label.HorizontalAlignment;
        rich.VerticalAlignment = label.VerticalAlignment;
        rich.AutowrapMode = label.AutowrapMode == TextServer.AutowrapMode.Off ? TextServer.AutowrapMode.Off : TextServer.AutowrapMode.WordSmart;
        rich.SizeFlagsHorizontal = label.SizeFlagsHorizontal;
        rich.SizeFlagsVertical = label.SizeFlagsVertical;
        rich.SizeFlagsStretchRatio = label.SizeFlagsStretchRatio;
        rich.CustomMinimumSize = label.CustomMinimumSize;
        rich.AnchorLeft = label.AnchorLeft; rich.AnchorTop = label.AnchorTop;
        rich.AnchorRight = label.AnchorRight; rich.AnchorBottom = label.AnchorBottom;
        rich.OffsetLeft = label.OffsetLeft; rich.OffsetTop = label.OffsetTop;
        rich.OffsetRight = label.OffsetRight; rich.OffsetBottom = label.OffsetBottom;
        rich.GrowHorizontal = label.GrowHorizontal; rich.GrowVertical = label.GrowVertical;
        if (label.HasThemeColorOverride("font_outline_color"))
            rich.AddThemeColorOverride("font_outline_color", label.GetThemeColor("font_outline_color"));
        if (label.HasThemeConstantOverride("outline_size"))
            rich.AddThemeConstantOverride("outline_size", label.GetThemeConstant("outline_size"));
        if (label.HasThemeColorOverride("font_shadow_color"))
            rich.AddThemeColorOverride("font_shadow_color", label.GetThemeColor("font_shadow_color"));
        Set(rich, label.Text);
        var parent = label.GetParent();
        if (parent != null)
        {
            int index = label.GetIndex();
            parent.RemoveChild(label);
            parent.AddChild(rich);
            parent.MoveChild(rich, index);
        }
        label.QueueFree();
        return rich;
    }

    /// <summary>
    /// Shows marked text on a <see cref="Button"/>. Buttons cannot draw BBCode, so when the text has an
    /// image the button text is cleared and a centred, mouse-transparent rich label child draws it;
    /// clicks, focus and the button's size stay with the button. Text without images is set normally.
    /// </summary>
    public static void SetButton(Button button, string text)
    {
        if (!GodotObject.IsInstanceValid(button)) return;
        var overlay = button.GetNodeOrNull<RichTextLabel>("PromptGlyphs");
        if (!HasImage(text))
        {
            if (overlay != null) overlay.Visible = false;
            button.Text = text;
            // The tooltip and saved plain text only exist for the image overlay; drop them with it.
            if (button.HasMeta(PlainMeta)) { button.RemoveMeta(PlainMeta); button.TooltipText = ""; }
            return;
        }
        if (overlay == null)
        {
            var font = button.GetThemeFont("font");
            overlay = CreateLabel(button.GetThemeFontSize("font_size"), button.GetThemeColor("font_color"), font);
            overlay.Name = "PromptGlyphs";
            overlay.FitContent = false;
            overlay.HorizontalAlignment = HorizontalAlignment.Center;
            overlay.VerticalAlignment = VerticalAlignment.Center;
            overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            overlay.OffsetLeft = 8; overlay.OffsetRight = -8;
            button.AddChild(overlay);
        }
        // With its text cleared, a button inside a container would shrink to nothing. Keep at least the
        // width the plain text needed, which is wider than the same prompt drawn with images.
        string plain = ToPlain(text);
        float width = button.GetThemeFont("font").GetStringSize(plain, HorizontalAlignment.Left, -1, button.GetThemeFontSize("font_size")).X + 24;
        button.CustomMinimumSize = new Vector2(Mathf.Max(button.CustomMinimumSize.X, width), button.CustomMinimumSize.Y);
        button.Text = "";
        overlay.Visible = true;
        Set(overlay, text);
        button.SetMeta(PlainMeta, plain);
        button.TooltipText = plain;
    }
}
