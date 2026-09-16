using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// TrainingManager handles the logic for Level 0: Protocol Zero.
/// It guides the player through a series of tutorials using a step-by-step system.
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

		public TutorialStep(string speaker, string type, string instruction, TutorialTarget target = null, bool reqAbility = false)
		{
			Speaker = speaker;
			Type = type;
			Instruction = instruction;
			Target = target;
			RequiresAbility = reqAbility;
		}
	}

	private List<TutorialStep> _steps = new();
	private int _currentStepIndex = 0;
	private DialogueUI _dialogueUI;
	private Player3D _player;

	public override async void _Ready()
	{
		_dialogueUI = GetTree().Root.FindChild("DialogueUI", true, false) as DialogueUI;
		_player = GetTree().Root.FindChild("Player3D", true, false) as Player3D;

		SetupSteps();
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
			GetNodeOrNull<TutorialTarget>("Target3"), true));

		_steps.Add(new TutorialStep("AXIOM-7", "vo",
			"Retrieve the calibration fragment to synchronize your data-link.",
			GetNodeOrNull<TutorialTarget>("Target4")));

		_steps.Add(new TutorialStep("AXIOM-7", "vo",
			"Protocol Zero nearly complete. Proceed to the extraction point for final deployment.",
			GetNodeOrNull<TutorialTarget>("Target5")));
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
			var levelManager = GetParentOrNull<Level3DManager>();
			levelManager?.OnLevelComplete();
			return;
		}

		var step = _steps[_currentStepIndex];

		// 1. Present instruction.
		await _dialogueUI.DisplayLine(step.Speaker, step.Instruction, step.Type);

		_player?.ResumeMovement();

		// 2. Wait for completion.
		if (step.Target != null)
		{
			var tcs = new TaskCompletionSource<bool>();
			Action handler = () => tcs.TrySetResult(true);
			step.Target.TargetReached += Callable.From(handler);

			await tcs.Task;

			// Clean up signal.
			step.Target.TargetReached -= Callable.From(handler);
		}
		else
		{
			// If no target, just a brief pause.
			await Task.Delay(2000);
		}

		_currentStepIndex++;
		_player?.StopMovement();
		await RunCurrentStep();
	}

	private Node GetParentOrNull<T>(string name) where T : Node
	{
		// Search for Level3DManager in the hierarchy.
		return GetTree().Root.FindChild("Level3DManager", true, false) as T;
	}
}
