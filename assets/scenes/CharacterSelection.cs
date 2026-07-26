using Godot;

/// <summary>
/// Character selection screen. Manages operative selection cards, updates live preview showcase,
/// creates a new session in SessionManager, and transitions to story intro.
/// </summary>
public partial class CharacterSelection : Control
{
	private Label _selectedNameLabel;
	private Label _classTagLabel;
	private AnimatedSprite2D _previewSprite;
	private SegmentedStatBar _speedMeter;
	private SegmentedStatBar _agilityMeter;

	private string _currentSelectedCharacter = "Ninja Frog";

	public override void _Ready()
	{
		GetNode<Node>("/root/Global").Call("play_menu_music");

		_selectedNameLabel = GetNode<Label>("MainLayout/RightShowcasePanel/VBox/SelectedName");
		_classTagLabel     = GetNode<Label>("MainLayout/RightShowcasePanel/VBox/ClassTag");
		_previewSprite     = GetNode<AnimatedSprite2D>("MainLayout/RightShowcasePanel/VBox/PreviewFrame/PreviewSprite");
		_speedMeter        = GetNode<SegmentedStatBar>("MainLayout/RightShowcasePanel/VBox/StatsBox/SpeedMeter");
		_agilityMeter      = GetNode<SegmentedStatBar>("MainLayout/RightShowcasePanel/VBox/StatsBox/AgilityMeter");

		SelectOperative("Ninja Frog");
		UISoundManager.AutoWireButtons(this);
	}

	public void _on_ninja_frog_pressed()    => SelectOperative("Ninja Frog");
	public void _on_mask_dude_pressed()     => SelectOperative("Mask Dude");
	public void _on_pink_man_pressed()      => SelectOperative("Pink Man");
	public void _on_virtual_guy_pressed()   => SelectOperative("Virtual Guy");
	public void _on_deploy_pressed()        => CreateSessionAndStart(_currentSelectedCharacter);

	private void SelectOperative(string characterName)
	{
		_currentSelectedCharacter = characterName;

		switch (characterName)
		{
			case "Ninja Frog":
				_selectedNameLabel.Text = Tr("Ninja Frog").ToUpper();
				_classTagLabel.Text     = Tr("// CLASS: RECON INFILTRATOR");
				_speedMeter?.SetStat(Tr("SPEED"), 92f);
				_agilityMeter?.SetStat(Tr("AGILITY"), 95f);
				break;
			case "Mask Dude":
				_selectedNameLabel.Text = Tr("Mask Dude").ToUpper();
				_classTagLabel.Text     = Tr("// CLASS: TACTICAL ASSAULT");
				_speedMeter?.SetStat(Tr("SPEED"), 85f);
				_agilityMeter?.SetStat(Tr("AGILITY"), 88f);
				break;
			case "Pink Man":
				_selectedNameLabel.Text = Tr("Pink Man").ToUpper();
				_classTagLabel.Text     = Tr("// CLASS: HEAVY SPECIALIST");
				_speedMeter?.SetStat(Tr("SPEED"), 78f);
				_agilityMeter?.SetStat(Tr("AGILITY"), 80f);
				break;
			case "Virtual Guy":
				_selectedNameLabel.Text = Tr("Virtual Guy").ToUpper();
				_classTagLabel.Text     = Tr("// CLASS: CYBERNETIC INFILTRATOR");
				_speedMeter?.SetStat(Tr("SPEED"), 90f);
				_agilityMeter?.SetStat(Tr("AGILITY"), 92f);
				break;
		}

		UpdatePreviewSprite(characterName);
	}

	private void UpdatePreviewSprite(string characterName)
	{
		var frames = new SpriteFrames();
		var tex    = GD.Load<Texture2D>("res://assets/images/" + characterName + "/Idle (32x32).png");
		if (tex == null) return;

		AddAnimationFrames(frames, "idle", tex, 11, 10f);
		_previewSprite.SpriteFrames = frames;
		_previewSprite.Play("idle");
	}

	private void CreateSessionAndStart(string characterName)
	{
		var global = GetNode<Node>("/root/Global");
		global.Set("selected_character", characterName);

		int sessionId = SessionManager.Instance.CreateNewSession(characterName);
		if (sessionId > 0)
			GD.Print("Created new session: ", sessionId, " for character: ", characterName);
		else
			GD.Print("Error creating session!");

		LoadingScreen.LoadScene(GetTree(), "res://assets/scenes/story_intro.tscn");
	}

	private static void AddAnimationFrames(
		SpriteFrames frames, string animName, Texture2D texture,
		int frameCount, float speed)
	{
		frames.AddAnimation(animName);
		frames.SetAnimationLoop(animName, true);
		frames.SetAnimationSpeed(animName, speed);

		for (int i = 0; i < frameCount; i++)
		{
			var atlas = new AtlasTexture { Atlas = texture };
			atlas.Region = new Rect2(i * 32, 0, 32, 32);
			frames.AddFrame(animName, atlas);
		}
	}
}
