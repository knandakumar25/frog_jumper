using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// TrainingManager handles the logic for Level 0: Protocol Zero.
/// It guides the player through a series of tutorials using a step-by-step system.
///
/// Sequencing: Level3DManager (this node's parent) plays its narrative briefing
/// first and emits BriefingFinished; the tutorial starts only afterwards, so the
/// briefing and the step instructions cannot interleave on the shared DialogueUI.
/// A step whose RequiresAbility is set additionally waits for its KillTarget's
/// Health.Died signal, so the target must actually be destroyed -- proximity alone
/// is not enough for the combat step.
/// </summary>
public partial class TrainingManager : Node
{
	private struct TutorialStep
	{
		public string Instruction;
		public string Speaker;
		public string Type;
		public TutorialTarget Target;
		public bool RequiresAbility;
		public Health KillTarget;
		public string PostLine;
		public string PostType;
		public string PostSpeaker;

		public TutorialStep(string speaker, string type, string instruction, TutorialTarget target = null,
			bool reqAbility = false, Health kill = null, string postLine = "", string postType = "vo", string postSpeaker = "AXIOM-7")
		{
			Speaker = speaker;
			Type = type;
			Instruction = instruction;
			Target = target;
			RequiresAbility = reqAbility;
			KillTarget = kill;
			PostLine = postLine;
			PostType = postType;
			PostSpeaker = postSpeaker;
		}
	}

	private List<TutorialStep> _steps = new();
	private int _currentStepIndex = 0;
	private DialogueUI _dialogueUI;
	private Player3D _player;
	private Level3DManager _levelManager;
	private MissionSummaryUI _summaryUI;

	public override async void _Ready()
	{
		_dialogueUI = GetTree().Root.FindChild("DialogueUI", true, false) as DialogueUI;
		_player = GetTree().Root.FindChild("Player3D", true, false) as Player3D;
		_levelManager = GetParentOrNull<Level3DManager>();
		_summaryUI = GetTree().Root.FindChild("MissionSummaryUI", true, false) as MissionSummaryUI;

		SetupSteps();

		// Hold the tutorial until the narrative briefing has finished, so both
		// dialogue consumers do not write over each other.
		if (_levelManager != null)
		{
			await ToSignal(_levelManager, Level3DManager.SignalName.BriefingFinished);
		}
		await StartTutorial();
	}

	private void SetupSteps()
	{
		// Define the training sequence.
		// In a real scene, these targets would be pre-placed in the editor.
		// Here, we assume they are children of the TrainingManager or designated nodes.

		_steps.Add(new TutorialStep("AXIOM-7", "vo",
			"Welcome to the Virtual Forge. Use WASD to move and navigate the grid.",
			GetNodeOrNull<TutorialTarget>("Target1")));

		_steps.Add(new TutorialStep("AXIOM-7", "vo",
			"Now, test your verticality. Jump over the digital divide.",
			GetNodeOrNull<TutorialTarget>("Target2")));

		_steps.Add(new TutorialStep("AXIOM-7", "vo",
			"Initialize your combat systems. Use your primary ability to neutralize the target.",
			GetNodeOrNull<TutorialTarget>("Target3"), true,
			GetNodeOrNull<Health>("TrainingDrone/Hitbox/Health"),
			"Target destroyed. All systems nominal. Calibration 100% complete. Prepare for field deployment into active combat sectors."));

		_steps.Add(new TutorialStep("AXIOM-7", "vo",
			"Retrieve the calibration fragment to synchronize your data-link.",
			GetNodeOrNull<TutorialTarget>("Target4")));

		_steps.Add(new TutorialStep("AXIOM-7", "vo",
			"Protocol Zero nearly complete. Proceed to the extraction point for final deployment.",
			GetNodeOrNull<TutorialTarget>("Target5"), false, null,
			"CALIBRATION COMPLETE. OPERATIVE CLEARED FOR FIELD DEPLOYMENT.", "title_card", "DISPLAY"));
	}

	private async Task StartTutorial()
	{
		if (_dialogueUI == null) return;

		_player?.StopMovement();
		await RunCurrentStep();
	}

	private async Task RunCurrentStep()
	{
		if (_currentStepIndex >= _steps.Count)
		{
			GD.Print("Training Complete!");
			// Trigger level completion.
			_levelManager?.OnLevelComplete();

			// Close the dialogue and hand over to the mission summary.
			_dialogueUI?.HideUI();
			_summaryUI?.ShowSummary(_levelManager?.CurrentPoints ?? 0, _levelManager?.CollectedCount ?? 0);
			return;
		}

		var step = _steps[_currentStepIndex];

		// 1. Present instruction.
		if (_dialogueUI != null)
		{
			await _dialogueUI.DisplayLine(step.Speaker, step.Instruction, step.Type);
		}

		_player?.ResumeMovement();

		// 2. Wait for completion.
		if (step.Target != null)
		{
			var tcs = new TaskCompletionSource<bool>();
			TutorialTarget.TargetReachedEventHandler handler = () => tcs.TrySetResult(true);
			step.Target.TargetReached += handler;

			await tcs.Task;

			// Clean up signal.
			step.Target.TargetReached -= handler;
		}
		else
		{
			// If no target, just a brief pause.
			await Task.Delay(2000);
		}

		// 2b. A combat step additionally requires its target to be destroyed.
		if (step.RequiresAbility && step.KillTarget != null)
		{
			var killTcs = new TaskCompletionSource<bool>();
			Health.DiedEventHandler killHandler = () => killTcs.TrySetResult(true);
			step.KillTarget.Died += killHandler;

			await killTcs.Task;

			step.KillTarget.Died -= killHandler;

			if (_dialogueUI != null && !string.IsNullOrEmpty(step.PostLine))
			{
				await _dialogueUI.DisplayLine(step.PostSpeaker, step.PostLine, step.PostType);
			}
		}
		else if (!string.IsNullOrEmpty(step.PostLine) && _dialogueUI != null)
		{
			await _dialogueUI.DisplayLine(step.PostSpeaker, step.PostLine, step.PostType);
		}

		_currentStepIndex++;
		_player?.StopMovement();
		await RunCurrentStep();
	}
}