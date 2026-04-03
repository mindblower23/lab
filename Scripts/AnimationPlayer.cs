using Godot;
using System;

public partial class AnimationPlayer : Godot.AnimationPlayer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Play("PlayerMotions/Walking");
		var timer = GetTree().CreateTimer(2.0);
		timer.Timeout += () => Play("PlayerMotions/Idle1", 0.5f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
