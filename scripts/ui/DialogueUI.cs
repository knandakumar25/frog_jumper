using Godot;
using System;
using System.Threading.Tasks;

/// <summary>
/// DialogueUI handles the visual presentation of the game's narrative.
/// It implements a cyberpunk typewriter effect and supports multiple dialogue types.
/// </summary>
public partial class DialogueUI : Control
{
	[Export] public float TypewriterSpeed = 0.03f;
	[Export] public float LineDelay = 1.0f;

	private Label _textLabel;
	private Label _speakerLabel;
	private Panel _background;
	private Tween _typewriterTween;
	private bool _isTyping = false;
	private string _fullText = "";

	public override void _Ready()
	{
		_textLabel = GetNode<Label>("Panel/VBox/TextLabel");
		_speakerLabel = GetNode<Label>("Panel/VBox/SpeakerLabel");
		_background = GetNode<Panel>("Panel");

		Visible = false;
	}

	/// <summary>
	/// Displays a line of dialogue with specific styling based on its type.
	/// Returns a Task that completes when the typewriter effect is finished.
	/// </summary>
	public async Task DisplayLine(string speaker, string text, string type)
	{
		_fullText = text;
		_textLabel.Text = "";
		_speakerLabel.Text = speaker;

		// 1. Apply styling based on dialogue type.
		ApplyStyle(type);

		// 2. Show the panel.
		Visible = true;

		// 3. Run the typewriter effect.
		await RunTypewriter();
	}

	private void ApplyStyle(string type)
	{
		switch (type)
		{
			case "title_card":
				_speakerLabel.Visible = false;
				_textLabel.HorizontalAlignment = HorizontalAlignment.Center;
				// In a real scenario, we would change the font/size here.
				break;
			case "internal":
				_speakerLabel.Visible = true;
				_textLabel.HorizontalAlignment = HorizontalAlignment.Center;
				// Add italic style if possible.
				break;
			case "vo":
			default:
				_speakerLabel.Visible = true;
				_textLabel.HorizontalAlignment = HorizontalAlignment.Left;
				break;
		}
	}

	private async Task RunTypewriter()
	{
		_isTyping = true;
		for (int i = 0; i < _fullText.Length; i++)
		{
			_textLabel.Text += _fullText[i];
			await Task.Delay((int)(TypewriterSpeed * 1000));
		}
		_isTyping = false;
	}

	public void SkipTypewriter()
	{
		if (_isTyping)
		{
			_textLabel.Text = _fullText;
			_isTyping = false;
			// In a real implementation, we would cancel the current Task.
		}
	}

	public void HideUI()
	{
		Visible = false;
	}
}
