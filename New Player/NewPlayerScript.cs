using Godot;
using System;

public partial class NewPlayerScript : Node3D
{

    [Export] public CharacterMotions CurrentMotion { get; set; } = CharacterMotions.Idle;
	[Export] public CharacterEmotions CurrentEmotion { get; set; } = CharacterEmotions.Neutral;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Key0))
		{
			CurrentMotion = CharacterMotions.Idle;
		}
		else if (Input.IsKeyPressed(Key.Key1))
		{
			CurrentMotion = CharacterMotions.Walk;
		}
		else if (Input.IsKeyPressed(Key.Key2))
		{
			CurrentMotion = CharacterMotions.Emote;
		}
		if (Input.IsKeyPressed(Key.Key3))
		{
			CurrentEmotion = CharacterEmotions.Neutral;
		}
		if (Input.IsKeyPressed(Key.Key4))
		{
			CurrentEmotion = CharacterEmotions.Sad;
		}
	}

	public void SetMotion(int motion)
	{
		CurrentMotion = (CharacterMotions)motion;
	}
	public void SetEmotion(int emotion)
	{
		CurrentEmotion = (CharacterEmotions)emotion;
	}
}
